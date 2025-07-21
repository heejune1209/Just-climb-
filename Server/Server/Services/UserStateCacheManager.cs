using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Server.Config;
using Server.Models;
using System;
using System.Threading.Tasks;

namespace Server.Services
{
    /// <summary>
    /// 사용자 상태 Redis 캐시 관리 전담 서비스
    /// </summary>
    public class UserStateCacheManager
    {
        private readonly IDistributedCache _cache;
        private readonly RedisSyncConfig _redisConfig;
        private readonly ILogger<UserStateCacheManager> _logger;

        public UserStateCacheManager(
            IDistributedCache cache,
            IOptions<RedisSyncConfig> redisOptions,
            ILogger<UserStateCacheManager> logger)
        {
            _cache = cache;
            _redisConfig = redisOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// 캐시에서 사용자 상태 조회
        /// </summary>
        public async Task<SaveData> GetCachedStateAsync(string userId)
        {
            try
            {
                string cacheKey = GetCacheKey(userId);
                var json = await _cache.GetStringAsync(cacheKey);
                
                if (!string.IsNullOrEmpty(json))
                {
                    _logger.LogDebug("[UserStateCacheManager] 캐시 hit - UserId: {UserId}", userId);
                    return JsonConvert.DeserializeObject<SaveData>(json);
                }

                _logger.LogDebug("[UserStateCacheManager] 캐시 miss - UserId: {UserId}", userId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserStateCacheManager] 캐시 조회 실패 - UserId: {UserId}", userId);
                return null; // 캐시 실패 시 null 반환하여 DB에서 로드하도록 함
            }
        }

        /// <summary>
        /// 사용자 상태를 캐시에 저장
        /// </summary>
        public async Task CacheStateAsync(string userId, SaveData saveData)
        {
            try
            {
                string cacheKey = GetCacheKey(userId);
                string json = JsonConvert.SerializeObject(saveData);

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_redisConfig.CacheDurationHours),
                    SlidingExpiration = TimeSpan.FromMinutes(_redisConfig.SlidingExpirationMinutes)
                };

                await _cache.SetStringAsync(cacheKey, json, cacheOptions);
                
                _logger.LogDebug("[UserStateCacheManager] 캐시 저장 완료 - UserId: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserStateCacheManager] 캐시 저장 실패 - UserId: {UserId}", userId);
                // 캐시 실패는 치명적이지 않으므로 예외를 던지지 않음
            }
        }

        /// <summary>
        /// 사용자 캐시 무효화
        /// </summary>
        public async Task InvalidateCacheAsync(string userId)
        {
            try
            {
                string cacheKey = GetCacheKey(userId);
                await _cache.RemoveAsync(cacheKey);
                
                _logger.LogDebug("[UserStateCacheManager] 캐시 무효화 완료 - UserId: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserStateCacheManager] 캐시 무효화 실패 - UserId: {UserId}", userId);
            }
        }

        /// <summary>
        /// 사용자 상태 캐시 갱신
        /// </summary>
        public async Task RefreshCacheAsync(string userId, SaveData saveData)
        {
            // 기존 캐시 무효화 후 새로운 데이터로 캐시
            await InvalidateCacheAsync(userId);
            await CacheStateAsync(userId, saveData);
        }

        /// <summary>
        /// 캐시 키 생성
        /// </summary>
        private string GetCacheKey(string userId)
        {
            return $"user_state:{userId}";
        }

        /// <summary>
        /// 캐시 상태 확인
        /// </summary>
        public async Task<bool> IsCachedAsync(string userId)
        {
            try
            {
                string cacheKey = GetCacheKey(userId);
                var cached = await _cache.GetStringAsync(cacheKey);
                return !string.IsNullOrEmpty(cached);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserStateCacheManager] 캐시 상태 확인 실패 - UserId: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 캐시 통계 (개발/디버깅용)
        /// </summary>
        public async Task<CacheStats> GetCacheStatsAsync(string userId)
        {
            var isCached = await IsCachedAsync(userId);
            
            return new CacheStats
            {
                UserId = userId,
                IsCached = isCached,
                CacheKey = GetCacheKey(userId),
                ExpirationHours = _redisConfig.CacheDurationHours,
                SlidingExpirationMinutes = _redisConfig.SlidingExpirationMinutes
            };
        }
    }

    /// <summary>
    /// 캐시 통계 정보
    /// </summary>
    public class CacheStats
    {
        public string UserId { get; set; }
        public bool IsCached { get; set; }
        public string CacheKey { get; set; }
        public int ExpirationHours { get; set; }
        public int SlidingExpirationMinutes { get; set; }
    }
} 