using Server.Models;

namespace Server.Services
{
    /// <summary>
    /// Redis 기반 고성능 랭킹 시스템 인터페이스
    /// </summary>
    public interface IRedisRankingService
    {
        /// <summary>
        /// 사용자 기록 실시간 업데이트 (O(log n) 성능)
        /// </summary>
        Task<bool> UpdateUserRecordAsync(string userId, UpdateRecordRequestDto request);

        /// <summary>
        /// 실시간 랭킹 조회 (O(log n) 성능)
        /// </summary>
        Task<RankingResponseDto> GetRankingAsync(string? currentUserId, RankingRequestDto request);

        /// <summary>
        /// DB에서 Redis로 랭킹 데이터 마이그레이션
        /// </summary>
        Task<bool> MigrateFromDatabaseAsync(IRankingService databaseRankingService, int stageNumber);

        /// <summary>
        /// Redis 랭킹 데이터 초기화
        /// </summary>
        Task<bool> InitializeAsync();

        /// <summary>
        /// Redis 연결 상태 체크
        /// </summary>
        Task<bool> IsHealthyAsync();
    }
} 