namespace Server.Config
{
    /// <summary>
    /// Redis 동기화 캐시 설정을 바인딩할 DTO입니다.
    /// Config/RedisSyncConfig.json의 "RedisSyncConfig" 섹션과 매핑됩니다.
    /// </summary>
    public class RedisSyncConfig
    {
        /// <summary>
        /// 델타 캐시 보관 시간(단위: 시간)
        /// </summary>
        public int CacheDurationHours { get; set; }
        
        /// <summary>
        /// 슬라이딩 만료 시간(단위: 분)
        /// </summary>
        public int SlidingExpirationMinutes { get; set; } = 30;
    }
}
