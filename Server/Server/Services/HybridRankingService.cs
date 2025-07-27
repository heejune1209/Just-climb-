using Server.Models;
using Microsoft.Extensions.Logging;

namespace Server.Services
{
    /// <summary>
    /// Redis(실시간) + Database(영구저장) 하이브리드 랭킹 시스템
    /// 최고의 성능과 데이터 안정성을 보장합니다.
    /// </summary>
    public class HybridRankingService : IRankingService
    {
        private readonly IRedisRankingService _redisRankingService;
        private readonly DatabaseRankingService _databaseRankingService;
        private readonly ILogger<HybridRankingService> _logger;

        public HybridRankingService(
            IRedisRankingService redisRankingService,
            DatabaseRankingService databaseRankingService,
            ILogger<HybridRankingService> logger)
        {
            _redisRankingService = redisRankingService;
            _databaseRankingService = databaseRankingService;
            _logger = logger;
        }

        /// <summary>
        /// 사용자 기록 업데이트: Redis(실시간) + DB(영구저장)
        /// </summary>
        public async Task<bool> UpdateUserRecordAsync(string userId, UpdateRecordRequestDto request)
        {
            try
            {
                _logger.LogInformation("🔄 하이브리드 랭킹 업데이트 시작: UserId={UserId}, Stage={Stage}", userId, request.StageNumber);

                // 1. Redis 실시간 업데이트 (O(log n) 고속 처리)
                var redisSuccess = await _redisRankingService.UpdateUserRecordAsync(userId, request);
                
                // 2. Database 영구 저장 (비동기 처리로 성능 영향 최소화)
                var dbTask = Task.Run(async () =>
                {
                    try
                    {
                        return await _databaseRankingService.UpdateUserRecordAsync(userId, request);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "🚨 DB 랭킹 업데이트 실패 (Redis는 성공): UserId={UserId}, Stage={Stage}", userId, request.StageNumber);
                        return false;
                    }
                });

                // Redis 성공하면 즉시 응답 (실시간 성능 우선)
                if (redisSuccess)
                {
                    _logger.LogInformation("✅ Redis 랭킹 업데이트 성공, DB 저장은 백그라운드 처리: UserId={UserId}", userId);
                    
                    // DB 저장 결과는 백그라운드에서 로깅만
                    _ = dbTask.ContinueWith(task =>
                    {
                        if (task.Result)
                        {
                            _logger.LogInformation("✅ DB 랭킹 백그라운드 저장 성공: UserId={UserId}", userId);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ DB 랭킹 백그라운드 저장 실패: UserId={UserId}", userId);
                        }
                    });
                    
                    return true;
                }
                else
                {
                    // Redis 실패 시 DB로 폴백
                    _logger.LogWarning("⚠️ Redis 업데이트 실패, DB로 폴백: UserId={UserId}", userId);
                    return await dbTask;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 하이브리드 랭킹 업데이트 전체 실패: UserId={UserId}, Stage={Stage}", userId, request.StageNumber);
                return false;
            }
        }

        /// <summary>
        /// 랭킹 조회: Redis 우선, 실패 시 DB 폴백
        /// </summary>
        public async Task<RankingResponseDto> GetRankingAsync(string? currentUserId, RankingRequestDto request)
        {
            try
            {
                _logger.LogDebug("🔍 하이브리드 랭킹 조회 시작: Stage={Stage}, SortType={SortType}", request.StageNumber, request.SortType);

                // 1. Redis에서 고속 조회 시도
                var redisResult = await _redisRankingService.GetRankingAsync(currentUserId, request);
                
                // Redis에 데이터가 있으면 즉시 반환 (초고속 응답)
                if (redisResult.TopEntries?.Any() == true)
                {
                    _logger.LogDebug("✅ Redis에서 랭킹 조회 성공: Stage={Stage}, Count={Count}", 
                        request.StageNumber, redisResult.TopEntries.Count);
                    return redisResult;
                }

                // 2. Redis에 데이터가 없으면 DB에서 조회 후 Redis에 캐시
                _logger.LogInformation("⚠️ Redis에 랭킹 데이터 없음, DB에서 조회 후 마이그레이션: Stage={Stage}", request.StageNumber);
                
                var dbResult = await _databaseRankingService.GetRankingAsync(currentUserId, request);
                
                // DB 결과를 Redis로 마이그레이션 (백그라운드)
                if (dbResult.TopEntries?.Any() == true)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _redisRankingService.MigrateFromDatabaseAsync(_databaseRankingService, request.StageNumber);
                            _logger.LogInformation("✅ DB → Redis 마이그레이션 완료: Stage={Stage}", request.StageNumber);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "🚨 DB → Redis 마이그레이션 실패: Stage={Stage}", request.StageNumber);
                        }
                    });
                }

                return dbResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 하이브리드 랭킹 조회 실패: Stage={Stage}", request.StageNumber);
                
                // 최종 폴백: 빈 응답 반환
                return new RankingResponseDto
                {
                    StageNumber = request.StageNumber,
                    SortType = request.SortType,
                    TopEntries = new List<RankingEntry>(),
                    TotalCount = 0
                };
            }
        }

        /// <summary>
        /// 서버 시작 시 DB → Redis 전체 마이그레이션
        /// </summary>
        public async Task<bool> InitializeRedisFromDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("🚀 서버 시작: DB → Redis 전체 랭킹 데이터 마이그레이션 시작");

                var tasks = new List<Task<bool>>();
                
                // 모든 스테이지(1~8)에 대해 마이그레이션
                for (int stage = 1; stage <= 8; stage++)
                {
                    tasks.Add(_redisRankingService.MigrateFromDatabaseAsync(_databaseRankingService, stage));
                }

                var results = await Task.WhenAll(tasks);
                var successCount = results.Count(r => r);

                _logger.LogInformation("✅ DB → Redis 전체 마이그레이션 완료: {Success}/{Total} 스테이지 성공", 
                    successCount, results.Length);

                return successCount == results.Length;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 DB → Redis 전체 마이그레이션 실패");
                return false;
            }
        }

        /// <summary>
        /// Redis 상태 체크 및 자동 복구
        /// </summary>
        public async Task<HealthCheckResult> CheckRedisHealthAsync()
        {
            try
            {
                var isHealthy = await _redisRankingService.IsHealthyAsync();
                
                return new HealthCheckResult
                {
                    IsHealthy = isHealthy,
                    Message = isHealthy ? "Redis 랭킹 시스템 정상 작동" : "Redis 연결 불안정"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 Redis 상태 체크 실패");
                
                return new HealthCheckResult
                {
                    IsHealthy = false,
                    Message = $"Redis 연결 실패: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// 헬스 체크 결과
    /// </summary>
    public class HealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public string Message { get; set; } = string.Empty;
    }
} 