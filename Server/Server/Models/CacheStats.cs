namespace Server.Models
{
    /// <summary>
    /// 캐시 통계 정보를 담는 DTO
    /// </summary>
    public class CacheStats
    {
        /// <summary>
        /// 사용자 ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 캐시되어 있는지 여부
        /// </summary>
        public bool IsCached { get; set; }

        /// <summary>
        /// 캐시 키
        /// </summary>
        public string CacheKey { get; set; }

        /// <summary>
        /// 만료 시간 (시간)
        /// </summary>
        public int ExpirationHours { get; set; }

        /// <summary>
        /// 슬라이딩 만료 시간 (분)
        /// </summary>
        public int SlidingExpirationMinutes { get; set; }

        /// <summary>
        /// 캐시 생성 시간 (선택사항)
        /// </summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// 마지막 액세스 시간 (선택사항)
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }
    }
} 