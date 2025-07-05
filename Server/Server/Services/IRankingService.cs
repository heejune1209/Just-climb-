using Server.Models;

namespace Server.Services
{
    /// <summary>
    /// 랭킹 시스템 서비스 인터페이스
    /// </summary>
    public interface IRankingService
    {
        /// <summary>
        /// 사용자 기록 업데이트 또는 생성
        /// </summary>
        Task<bool> UpdateUserRecordAsync(string userId, UpdateRecordRequestDto request);

        /// <summary>
        /// 스테이지별 랭킹 조회 (페이징 지원)
        /// </summary>
        Task<RankingResponseDto> GetRankingAsync(string? currentUserId, RankingRequestDto request);
    }
} 