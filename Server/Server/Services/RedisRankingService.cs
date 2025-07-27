using StackExchange.Redis;
using Server.Models;
using Microsoft.Extensions.Logging;

namespace Server.Services
{
    /// <summary>
    /// Redis Sorted Set을 활용한 고성능 랭킹 시스템
    /// O(log n) 시간 복잡도로 실시간 랭킹 처리
    /// </summary>
    public class RedisRankingService : IRedisRankingService
    {
        private readonly IDatabase _redis;
        private readonly ILogger<RedisRankingService> _logger;
        
        public RedisRankingService(IConnectionMultiplexer redis, ILogger<RedisRankingService> logger)
        {
            _redis = redis.GetDatabase();
            _logger = logger;
        }

        #region Redis Keys
        private string GetClearTimeRankingKey(int stageNumber) 
            => $"ranking:stage:{stageNumber}:cleartime";
        
        private string GetDeathCountRankingKey(int stageNumber) 
            => $"ranking:stage:{stageNumber}:deathcount";
        
        private string GetUserDataKey(string userId, int stageNumber) 
            => $"userdata:stage:{stageNumber}:user:{userId}";
        #endregion

        /// <summary>
        /// 사용자 기록 실시간 업데이트 (O(log n) 성능)
        /// </summary>
        public async Task<bool> UpdateUserRecordAsync(string userId, UpdateRecordRequestDto request)
        {
            try
            {
                var clearTimeKey = GetClearTimeRankingKey(request.StageNumber);
                var deathCountKey = GetDeathCountRankingKey(request.StageNumber);
                var userDataKey = GetUserDataKey(userId, request.StageNumber);

                // Redis Transaction으로 원자적 업데이트
                var transaction = _redis.CreateTransaction();
                
                // Sorted Set에 기록 추가/업데이트 (O(log n))
                transaction.SortedSetAddAsync(clearTimeKey, userId, request.ClearTime);
                transaction.SortedSetAddAsync(deathCountKey, userId, request.DeathCount);
                
                // 사용자 상세 정보 저장 (Hash)
                transaction.HashSetAsync(userDataKey, new HashEntry[]
                {
                    new("clearTime", request.ClearTime),
                    new("deathCount", request.DeathCount),
                    new("displayName", request.DisplayName ?? "Player"),
                    new("updatedAt", DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                });

                // TTL 설정 (7일간 유지)
                transaction.KeyExpireAsync(clearTimeKey, TimeSpan.FromDays(7));
                transaction.KeyExpireAsync(deathCountKey, TimeSpan.FromDays(7));
                transaction.KeyExpireAsync(userDataKey, TimeSpan.FromDays(7));

                bool success = await transaction.ExecuteAsync();
                
                if (success)
                {
                    _logger.LogInformation("✅ Redis 랭킹 업데이트 성공: UserId={UserId}, Stage={Stage}, Time={Time}, Deaths={Deaths}",
                        userId, request.StageNumber, request.ClearTime, request.DeathCount);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 Redis 랭킹 업데이트 실패: UserId={UserId}, Stage={Stage}", userId, request.StageNumber);
                return false;
            }
        }

        /// <summary>
        /// 실시간 랭킹 조회 (O(log n) 성능)
        /// </summary>
        public async Task<RankingResponseDto> GetRankingAsync(string? currentUserId, RankingRequestDto request)
        {
            try
            {
                var sortType = (RankingSortType)request.SortType;
                var key = sortType == RankingSortType.ClearTime 
                    ? GetClearTimeRankingKey(request.StageNumber)
                    : GetDeathCountRankingKey(request.StageNumber);

                // 상위 N개 조회 (O(log n))
                var topEntries = await _redis.SortedSetRangeByRankWithScoresAsync(
                    key, 
                    start: (request.Page - 1) * request.PageSize, 
                    stop: request.Page * request.PageSize - 1,
                    order: Order.Ascending
                );

                // 전체 참가자 수 (O(1))
                var totalCount = await _redis.SortedSetLengthAsync(key);

                // 내 순위 조회 (O(log n))
                RankingEntry? myEntry = null;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var myRank = await _redis.SortedSetRankAsync(key, currentUserId, Order.Ascending);
                    if (myRank.HasValue)
                    {
                        var myScore = await _redis.SortedSetScoreAsync(key, currentUserId);
                        var myData = await GetUserDataAsync(currentUserId, request.StageNumber);
                        
                        myEntry = new RankingEntry
                        {
                            Rank = (int)myRank.Value + 1,
                            UserId = currentUserId,
                            DisplayName = myData?.DisplayName ?? "Player",
                            ClearTime = sortType == RankingSortType.ClearTime ? (float)(myScore ?? 0) : myData?.ClearTime ?? 0,
                            DeathCount = sortType == RankingSortType.DeathCount ? (int)(myScore ?? 0) : myData?.DeathCount ?? 0,
                            IsMyRecord = true,
                            UpdatedAt = myData?.UpdatedAt ?? DateTime.UtcNow
                        };
                    }
                }

                // 상위 랭킹 엔트리 구성
                var rankingEntries = new List<RankingEntry>();
                for (int i = 0; i < topEntries.Length; i++)
                {
                    var entry = topEntries[i];
                    var userData = await GetUserDataAsync(entry.Element, request.StageNumber);
                    
                    rankingEntries.Add(new RankingEntry
                    {
                        Rank = (request.Page - 1) * request.PageSize + i + 1,
                        UserId = entry.Element,
                        DisplayName = userData?.DisplayName ?? "Player",
                        ClearTime = sortType == RankingSortType.ClearTime ? (float)entry.Score : userData?.ClearTime ?? 0,
                        DeathCount = sortType == RankingSortType.DeathCount ? (int)entry.Score : userData?.DeathCount ?? 0,
                        IsMyRecord = entry.Element == currentUserId,
                        UpdatedAt = userData?.UpdatedAt ?? DateTime.UtcNow
                    });
                }

                return new RankingResponseDto
                {
                    StageNumber = request.StageNumber,
                    SortType = request.SortType,
                    TopEntries = rankingEntries,
                    MyEntry = myEntry,
                    TotalCount = (int)totalCount,
                    CurrentPage = request.Page,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                    HasNextPage = request.Page * request.PageSize < totalCount,
                    HasPreviousPage = request.Page > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Redis 랭킹 조회 실패: Stage={Stage}", request.StageNumber);
                return new RankingResponseDto
                {
                    StageNumber = request.StageNumber,
                    SortType = request.SortType
                };
            }
        }

        /// <summary>
        /// DB에서 Redis로 랭킹 데이터 마이그레이션
        /// </summary>
        public async Task<bool> MigrateFromDatabaseAsync(IRankingService databaseRankingService, int stageNumber)
        {
            try
            {
                _logger.LogInformation("Stage {Stage} 랭킹 데이터 마이그레이션 시작", stageNumber);

                // DB에서 모든 랭킹 데이터 조회
                var request = new RankingRequestDto 
                { 
                    StageNumber = stageNumber, 
                    Page = 1, 
                    PageSize = 10000, // 대량 조회
                    SortType = (int)RankingSortType.ClearTime 
                };

                var dbRanking = await databaseRankingService.GetRankingAsync(null, request);
                
                if (dbRanking.TopEntries?.Any() == true)
                {
                    var clearTimeKey = GetClearTimeRankingKey(stageNumber);
                    var deathCountKey = GetDeathCountRankingKey(stageNumber);

                    // 배치로 Redis에 추가
                    var transaction = _redis.CreateTransaction();
                    
                    foreach (var entry in dbRanking.TopEntries)
                    {
                        transaction.SortedSetAddAsync(clearTimeKey, entry.UserId, entry.ClearTime);
                        transaction.SortedSetAddAsync(deathCountKey, entry.UserId, entry.DeathCount);
                        
                        var userDataKey = GetUserDataKey(entry.UserId, stageNumber);
                        transaction.HashSetAsync(userDataKey, new HashEntry[]
                        {
                            new("clearTime", entry.ClearTime),
                            new("deathCount", entry.DeathCount),
                            new("displayName", entry.DisplayName),
                            new("updatedAt", new DateTimeOffset(entry.UpdatedAt).ToUnixTimeSeconds())
                        });
                    }

                    bool success = await transaction.ExecuteAsync();
                    
                    if (success)
                    {
                        _logger.LogInformation("Stage {Stage} 랭킹 마이그레이션 완료: {Count}개 기록", 
                            stageNumber, dbRanking.TopEntries.Count);
                    }

                    return success;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stage {Stage} 랭킹 마이그레이션 실패", stageNumber);
                return false;
            }
        }

        /// <summary>
        /// Redis 랭킹 데이터 초기화
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Redis 랭킹 시스템 초기화 시작");
                
                // Redis 연결 테스트
                await _redis.PingAsync();
                
                _logger.LogInformation("Redis 랭킹 시스템 초기화 완료");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis 랭킹 시스템 초기화 실패");
                return false;
            }
        }

        /// <summary>
        /// Redis 연결 상태 체크
        /// </summary>
        public async Task<bool> IsHealthyAsync()
        {
            try
            {
                var ping = await _redis.PingAsync();
                return ping.TotalMilliseconds < 1000; // 1초 이내 응답
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 사용자 상세 데이터 조회
        /// </summary>
        private async Task<UserRankingData?> GetUserDataAsync(string userId, int stageNumber)
        {
            try
            {
                var userDataKey = GetUserDataKey(userId, stageNumber);
                var hash = await _redis.HashGetAllAsync(userDataKey);
                
                if (hash.Length == 0) return null;

                var data = hash.ToDictionary(x => x.Name, x => x.Value);
                
                return new UserRankingData
                {
                    ClearTime = data.ContainsKey("clearTime") ? (float)data["clearTime"] : 0,
                    DeathCount = data.ContainsKey("deathCount") ? (int)data["deathCount"] : 0,
                    DisplayName = data.ContainsKey("displayName") ? data["displayName"] : "Player",
                    UpdatedAt = data.ContainsKey("updatedAt") 
                        ? DateTimeOffset.FromUnixTimeSeconds((long)data["updatedAt"]).DateTime
                        : DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Redis에서 조회한 사용자 랭킹 데이터
    /// </summary>
    public class UserRankingData
    {
        public float ClearTime { get; set; }
        public int DeathCount { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
    }
} 