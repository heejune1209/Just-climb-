using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Models;

namespace Server.Services
{
    /// <summary>
    /// 랭킹 시스템 서비스 구현
    /// 서버에서 정렬된 랭킹 데이터를 제공합니다.
    /// </summary>
    public class RankingService : IRankingService
    {
        private readonly JustClimbDbContext _context;
        private readonly ILogger<RankingService> _logger;

        public RankingService(JustClimbDbContext context, ILogger<RankingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 사용자 기록 업데이트 또는 생성 (UPSERT)
        /// </summary>
        public async Task<bool> UpdateUserRecordAsync(string userId, UpdateRecordRequestDto request)
        {
            try
            {
                var existingRecord = await _context.UserStageRecords
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.StageNumber == request.StageNumber);

                if (existingRecord == null)
                {
                    // 새 기록 생성
                    var newRecord = new UserStageRecord
                    {
                        UserId = userId,
                        StageNumber = request.StageNumber,
                        BestClearTime = request.ClearTime,
                        BestDeathCount = request.DeathCount,
                        DisplayName = request.DisplayName,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.UserStageRecords.Add(newRecord);
                }
                else
                {
                    // 기존 기록 업데이트 (더 좋은 기록일 때만)
                    bool updated = false;

                    // 더 빠른 클리어 타임
                    if (request.ClearTime > 0 && 
                        (existingRecord.BestClearTime <= 0 || request.ClearTime < existingRecord.BestClearTime))
                    {
                        existingRecord.BestClearTime = request.ClearTime;
                        updated = true;
                    }

                    // 더 적은 사망 횟수
                    if (existingRecord.BestDeathCount < 0 || request.DeathCount < existingRecord.BestDeathCount)
                    {
                        existingRecord.BestDeathCount = request.DeathCount;
                        updated = true;
                    }

                    // 표시 이름 업데이트
                    if (!string.IsNullOrEmpty(request.DisplayName) && 
                        existingRecord.DisplayName != request.DisplayName)
                    {
                        existingRecord.DisplayName = request.DisplayName;
                        updated = true;
                    }

                    if (updated)
                    {
                        existingRecord.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "사용자 {UserId} 스테이지 {Stage} 기록 업데이트 실패", userId, request.StageNumber);
                return false;
            }
        }

        /// <summary>
        /// 스테이지별 랭킹 조회 (서버에서 정렬됨)
        /// </summary>
        public async Task<RankingResponseDto> GetRankingAsync(string? currentUserId, RankingRequestDto request)
        {
            try
            {
                // 유효한 기록만 조회 (클리어 타임 > 0)
                var baseQuery = _context.UserStageRecords
                    .Where(r => r.StageNumber == request.StageNumber && r.BestClearTime > 0);

                // 정렬 기준에 따라 정렬
                IQueryable<UserStageRecord> sortedQuery = request.SortType switch
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
                    .Select((record, index) => new RankingEntryDto
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
                RankingEntryDto? myEntry = null;
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var myRecord = await GetUserRecordAsync(currentUserId, request.StageNumber);

                    if (myRecord != null && myRecord.BestClearTime > 0)
                    {
                        // 내 순위 계산
                        var myRank = await GetUserRankAsync(currentUserId, request.StageNumber, request.SortType);
                        
                        // 상위 N개에 포함되지 않은 경우에만 별도 표시
                        if (myRank > request.PageSize || !topEntries.Any(e => e.UserId == currentUserId))
                        {
                            myEntry = new RankingEntryDto
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
                _logger.LogError(ex, "스테이지 {Stage} 랭킹 조회 실패", request.StageNumber);
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
                if (userRecord == null || userRecord.BestClearTime <= 0)
                    return -1; // 기록 없음

                // 나보다 좋은 기록의 개수 + 1 = 내 순위
                var betterRecordsCount = sortType switch
                {
                    RankingSortType.ClearTime => await _context.UserStageRecords
                        .Where(r => r.StageNumber == stageNumber && r.BestClearTime > 0)
                        .Where(r => r.BestClearTime < userRecord.BestClearTime ||
                                   (r.BestClearTime == userRecord.BestClearTime && r.BestDeathCount < userRecord.BestDeathCount) ||
                                   (r.BestClearTime == userRecord.BestClearTime && r.BestDeathCount == userRecord.BestDeathCount && r.UpdatedAt < userRecord.UpdatedAt))
                        .CountAsync(),
                    RankingSortType.DeathCount => await _context.UserStageRecords
                        .Where(r => r.StageNumber == stageNumber && r.BestClearTime > 0)
                        .Where(r => r.BestDeathCount < userRecord.BestDeathCount ||
                                   (r.BestDeathCount == userRecord.BestDeathCount && r.BestClearTime < userRecord.BestClearTime) ||
                                   (r.BestDeathCount == userRecord.BestDeathCount && r.BestClearTime == userRecord.BestClearTime && r.UpdatedAt < userRecord.UpdatedAt))
                        .CountAsync(),
                    _ => await _context.UserStageRecords
                        .Where(r => r.StageNumber == stageNumber && r.BestClearTime > 0)
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