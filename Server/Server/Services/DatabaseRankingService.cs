using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Models;

namespace Server.Services
{
    /// <summary>
    /// 데이터베이스 기반 랭킹 시스템 서비스 (영구 저장 및 백업 용도)
    /// HybridRankingService에서 백엔드 저장소로 사용됩니다.
    /// Transient로 등록되어 각 요청마다 새로운 인스턴스가 생성됩니다.
    /// </summary>
    public class DatabaseRankingService : IRankingService
    {
        private readonly JustClimbDbContext _context;
        private readonly ILogger<DatabaseRankingService> _logger;

        public DatabaseRankingService(JustClimbDbContext context, ILogger<DatabaseRankingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 사용자 기록 업데이트 또는 생성 (UPSERT) - DB 영구 저장
        /// </summary>
        public async Task<bool> UpdateUserRecordAsync(string userId, UpdateRecordRequestDto request)
        {
            try
            {
                _logger.LogInformation("💾 DB 랭킹 업데이트 시작: UserId={UserId}, Stage={Stage}, Time={Time}, Deaths={Deaths}", 
                    userId, request.StageNumber, request.ClearTime, request.DeathCount);

                // 추가 데이터 검증
                if (string.IsNullOrEmpty(userId) || userId.Length > 100)
                {
                    _logger.LogError("유효하지 않은 UserId: {UserId}", userId);
                    return false;
                }

                if (request.ClearTime <= 0 || request.ClearTime >= float.MaxValue)
                {
                    _logger.LogError("유효하지 않은 ClearTime: {Time}", request.ClearTime);
                    return false;
                }

                if (request.DeathCount < 0 || request.DeathCount >= int.MaxValue)
                {
                    _logger.LogError("유효하지 않은 DeathCount: {Deaths}", request.DeathCount);
                    return false;
                }

                // 🔧 트랜잭션을 사용한 안전한 User 생성 및 기록 업데이트
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // User 존재 여부 확인 및 생성 (외래키 제약 조건 해결)
                    var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (existingUser == null)
                    {
                        _logger.LogInformation("User가 존재하지 않아 새로 생성: UserId={UserId}", userId);
                        
                        var newUser = new User
                        {
                            Id = userId,
                            Gold = 0,
                            Gems = 0,
                            SelectedCharacter = "Default",
                            TutorialDisplayed = false,
                            SteamDisplayName = request.DisplayName ?? "Player",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        
                        _context.Users.Add(newUser);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("User 생성 완료: UserId={UserId}", userId);
                    }

                var existingRecord = await _context.UserStageRecords
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.StageNumber == request.StageNumber);

                if (existingRecord == null)
                {
                    _logger.LogInformation("새 기록 생성: UserId={UserId}, Stage={Stage}", userId, request.StageNumber);
                    
                    // 새 기록 생성
                    var newRecord = new UserStageRecord
                    {
                        UserId = userId,
                        StageNumber = request.StageNumber,
                        IsCleared = true,
                        BestClearTime = request.ClearTime,
                        BestDeathCount = request.DeathCount,
                        DisplayName = request.DisplayName ?? "Player",
                        BestGemCount = 0,  // 기본값 설정
                        CurrentPlayTime = 0f,
                        CurrentDeathCount = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.UserStageRecords.Add(newRecord);
                }
                else
                {
                    _logger.LogInformation("기존 기록 업데이트 시도: UserId={UserId}, Stage={Stage}, 기존 Time={OldTime}, 새 Time={NewTime}", 
                        userId, request.StageNumber, existingRecord.BestClearTime, request.ClearTime);
                    
                    // 기존 기록 업데이트 (더 좋은 기록일 때만)
                    bool updated = false;

                    // 클리어 표시
                    if (!existingRecord.IsCleared)
                    {
                        existingRecord.IsCleared = true;
                        updated = true;
                        _logger.LogInformation("클리어 상태 업데이트: UserId={UserId}, Stage={Stage}", userId, request.StageNumber);
                    }

                    // 더 빠른 클리어 타임
                    if (request.ClearTime > 0 && 
                        (existingRecord.BestClearTime <= 0 || existingRecord.BestClearTime >= float.MaxValue || request.ClearTime < existingRecord.BestClearTime))
                    {
                        _logger.LogInformation("더 빠른 클리어 타임 업데이트: {OldTime} -> {NewTime}", existingRecord.BestClearTime, request.ClearTime);
                        existingRecord.BestClearTime = request.ClearTime;
                        updated = true;
                    }

                    // 더 적은 사망 횟수
                    if (existingRecord.BestDeathCount < 0 || existingRecord.BestDeathCount >= int.MaxValue || request.DeathCount < existingRecord.BestDeathCount)
                    {
                        _logger.LogInformation("더 적은 사망 횟수 업데이트: {OldDeaths} -> {NewDeaths}", existingRecord.BestDeathCount, request.DeathCount);
                        existingRecord.BestDeathCount = request.DeathCount;
                        updated = true;
                    }

                    // 표시 이름 업데이트
                    if (!string.IsNullOrEmpty(request.DisplayName) && 
                        existingRecord.DisplayName != request.DisplayName)
                    {
                        _logger.LogInformation("표시 이름 업데이트: {OldName} -> {NewName}", existingRecord.DisplayName, request.DisplayName);
                        existingRecord.DisplayName = request.DisplayName;
                        updated = true;
                    }

                    if (updated)
                    {
                        existingRecord.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _logger.LogInformation("기존 기록이 더 좋아서 업데이트 안함: UserId={UserId}, Stage={Stage}", userId, request.StageNumber);
                    }
                }

                _logger.LogInformation("데이터베이스 저장 시도: UserId={UserId}, Stage={Stage}", userId, request.StageNumber);
                await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                _logger.LogInformation("💾 DB 랭킹 저장 성공: UserId={UserId}, Stage={Stage}", userId, request.StageNumber);
                return true;
                }
                catch (Exception innerEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(innerEx, "트랜잭션 중 오류 발생, 롤백 실행: UserId={UserId}, Stage={Stage}", userId, request.StageNumber);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 DB 랭킹 업데이트 실패 - UserId: {UserId}, Stage: {Stage}, ClearTime: {ClearTime}, DeathCount: {DeathCount}, DisplayName: {DisplayName}\n" +
                    "Exception: {Exception}\n" +
                    "InnerException: {InnerException}\n" +
                    "StackTrace: {StackTrace}", 
                    userId, request.StageNumber, request.ClearTime, request.DeathCount, request.DisplayName,
                    ex.Message, ex.InnerException?.Message, ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// 스테이지별 랭킹 조회 (데이터베이스에서 정렬됨)
        /// </summary>
        public async Task<RankingResponseDto> GetRankingAsync(string? currentUserId, RankingRequestDto request)
        {
            try
            {
                // 유효한 기록만 조회 (클리어된 기록 + 클리어 타임 > 0 + MaxValue 제외)
                var baseQuery = _context.UserStageRecords
                    .Where(r => r.StageNumber == request.StageNumber && 
                               r.IsCleared && 
                               r.BestClearTime > 0 &&
                               r.BestClearTime < float.MaxValue &&  // MaxValue 제외
                               r.BestDeathCount < int.MaxValue);    // MaxValue 제외

                // 정렬 기준에 따라 정렬
                var sortType = (RankingSortType)request.SortType;
                IQueryable<UserStageRecord> sortedQuery = sortType switch
                {
                    RankingSortType.ClearTime => baseQuery
                        .OrderBy(r => r.BestClearTime)
                        .ThenBy(r => r.BestDeathCount)
                        .ThenBy(r => r.UpdatedAt),
                    RankingSortType.DeathCount => baseQuery
                        .OrderBy(r => r.BestDeathCount)
                        .ThenBy(r => r.BestClearTime)
                        .ThenBy(r => r.UpdatedAt),
                    _ => baseQuery.OrderBy(r => r.BestClearTime)
                };

                // 전체 개수
                var totalCount = await sortedQuery.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

                // 상위 N개 조회 (페이징)
                var topRecords = await sortedQuery
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                // 랭킹 항목으로 변환
                var topEntries = topRecords
                    .Select((record, index) => new RankingEntry
                    {
                        Rank = (request.Page - 1) * request.PageSize + index + 1,
                        UserId = record.UserId,
                        DisplayName = record.DisplayName,
                        ClearTime = record.BestClearTime,
                        DeathCount = record.BestDeathCount,
                        IsMyRecord = record.UserId == currentUserId,
                        UpdatedAt = record.UpdatedAt
                    })
                    .ToList();

                // 내 기록 조회 (상위 N개에 포함되지 않은 경우)
                RankingEntry? myEntry = null;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var myRecord = await GetUserRecordAsync(currentUserId, request.StageNumber);

                    if (myRecord != null && myRecord.IsCleared && 
                        myRecord.BestClearTime > 0 && 
                        myRecord.BestClearTime < float.MaxValue &&
                        myRecord.BestDeathCount < int.MaxValue)
                    {
                        // 내 순위 계산
                        var myRank = await GetUserRankAsync(currentUserId, request.StageNumber, sortType);
                        
                        // 상위 N개에 포함되지 않은 경우에만 별도 표시
                        if (myRank > request.PageSize || !topEntries.Any(e => e.UserId == currentUserId))
                        {
                            myEntry = new RankingEntry
                            {
                                Rank = myRank,
                                UserId = myRecord.UserId,
                                DisplayName = myRecord.DisplayName,
                                ClearTime = myRecord.BestClearTime,
                                DeathCount = myRecord.BestDeathCount,
                                IsMyRecord = true,
                                UpdatedAt = myRecord.UpdatedAt
                            };
                        }
                    }
                }

                return new RankingResponseDto
                {
                    StageNumber = request.StageNumber,
                    SortType = request.SortType,
                    TopEntries = topEntries,
                    MyEntry = myEntry,
                    TotalCount = totalCount,
                    CurrentPage = request.Page,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages,
                    HasPreviousPage = request.Page > 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "스테이지 {Stage} DB 랭킹 조회 실패", request.StageNumber);
                return new RankingResponseDto
                {
                    StageNumber = request.StageNumber,
                    SortType = request.SortType
                };
            }
        }

        /// <summary>
        /// 특정 사용자의 특정 스테이지 기록 조회 (내부 사용)
        /// </summary>
        private async Task<UserStageRecord?> GetUserRecordAsync(string userId, int stageNumber)
        {
            return await _context.UserStageRecords
                .FirstOrDefaultAsync(r => r.UserId == userId && r.StageNumber == stageNumber);
        }

        /// <summary>
        /// 특정 사용자의 특정 스테이지에서의 순위 조회 (내부 사용)
        /// </summary>
        private async Task<int> GetUserRankAsync(string userId, int stageNumber, RankingSortType sortType)
        {
            try
            {
                var userRecord = await GetUserRecordAsync(userId, stageNumber);
                if (userRecord == null || !userRecord.IsCleared || 
                    userRecord.BestClearTime <= 0 || 
                    userRecord.BestClearTime >= float.MaxValue ||
                    userRecord.BestDeathCount >= int.MaxValue)
                    return -1; // 기록 없음

                // 나보다 좋은 기록의 개수 + 1 = 내 순위 (MaxValue 제외)
                var betterRecordsCount = sortType switch
                {
                    RankingSortType.ClearTime => await _context.UserStageRecords
                        .Where(r => r.StageNumber == stageNumber && r.IsCleared && 
                                   r.BestClearTime > 0 && r.BestClearTime < float.MaxValue && r.BestDeathCount < int.MaxValue)
                        .Where(r => r.BestClearTime < userRecord.BestClearTime ||
                                   (r.BestClearTime == userRecord.BestClearTime && r.BestDeathCount < userRecord.BestDeathCount) ||
                                   (r.BestClearTime == userRecord.BestClearTime && r.BestDeathCount == userRecord.BestDeathCount && r.UpdatedAt < userRecord.UpdatedAt))
                        .CountAsync(),
                    RankingSortType.DeathCount => await _context.UserStageRecords
                        .Where(r => r.StageNumber == stageNumber && r.IsCleared && 
                                   r.BestClearTime > 0 && r.BestClearTime < float.MaxValue && r.BestDeathCount < int.MaxValue)
                        .Where(r => r.BestDeathCount < userRecord.BestDeathCount ||
                                   (r.BestDeathCount == userRecord.BestDeathCount && r.BestClearTime < userRecord.BestClearTime) ||
                                   (r.BestDeathCount == userRecord.BestDeathCount && r.BestClearTime == userRecord.BestClearTime && r.UpdatedAt < userRecord.UpdatedAt))
                        .CountAsync(),
                    _ => await _context.UserStageRecords
                        .Where(r => r.StageNumber == stageNumber && r.IsCleared && 
                                   r.BestClearTime > 0 && r.BestClearTime < float.MaxValue && r.BestDeathCount < int.MaxValue)
                        .Where(r => r.BestClearTime < userRecord.BestClearTime)
                        .CountAsync()
                };

                return betterRecordsCount + 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 {UserId} 스테이지 {Stage} 순위 조회 실패", userId, stageNumber);
                return -1;
            }
        }
    }
} 