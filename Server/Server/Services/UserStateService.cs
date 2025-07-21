using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Database;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Services
{
    /// <summary>
    /// 사용자 상태 관리 메인 Facade 서비스
    /// 분리된 서비스들을 조합하여 통합된 API를 제공합니다.
    /// </summary>
    public class UserStateService : IUserStateService
    {
        private readonly UserStateLoader _loader;
        private readonly UserStateCacheManager _cacheManager;
        private readonly DeltaProcessor _deltaProcessor;
        private readonly UserStateSaver _saver;
        private readonly JustClimbDbContext _dbContext;
        private readonly ILogger<UserStateService> _logger;

        public UserStateService(
            UserStateLoader loader,
            UserStateCacheManager cacheManager,
            DeltaProcessor deltaProcessor,
            UserStateSaver saver,
            JustClimbDbContext dbContext,
            ILogger<UserStateService> logger)
        {
            _loader = loader;
            _cacheManager = cacheManager;
            _deltaProcessor = deltaProcessor;
            _saver = saver;
            _dbContext = dbContext;
            _logger = logger;
        }

        #region IUserStateService Implementation

        /// <summary>
        /// 사용자 상태를 로드합니다 (캐시 우선, DB 보조)
        /// </summary>
        public async Task<SaveData> LoadStateAsync(string userId)
        {
            _logger.LogInformation("[UserStateService] LoadStateAsync 시작 - UserId: {UserId}", userId);
            
            try
            {
                var result = await _loader.LoadUserStateAsync(userId);
                
                _logger.LogInformation("[UserStateService] LoadStateAsync 완료 - UserId: {UserId}", userId);
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "[UserStateService] LoadStateAsync 실패 - UserId: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 사용자 상태를 저장합니다
        /// </summary>
        public async Task<bool> SaveStateAsync(string userId, SaveData saveData)
        {
            _logger.LogInformation("[UserStateService] SaveStateAsync 시작 - UserId: {UserId}", userId);
            
            try
            {
                var success = await _saver.SaveUserStateAsync(userId, saveData);
                
                if (success)
                {
                    // 캐시 갱신
                    await _cacheManager.RefreshCacheAsync(userId, saveData);
                }
                
                _logger.LogInformation("[UserStateService] SaveStateAsync 완료 - UserId: {UserId}, Success: {Success}", 
                    userId, success);
                return success;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "[UserStateService] SaveStateAsync 실패 - UserId: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 델타 이벤트들을 처리합니다
        /// </summary>
        public async Task MergeDeltasAsync(string userId, IEnumerable<DeltaEventDto> deltas)
        {
            var deltaList = deltas?.ToList() ?? new List<DeltaEventDto>();
            _logger.LogInformation("[UserStateService] MergeDeltasAsync 시작 - UserId: {UserId}, 델타 개수: {Count}", 
                userId, deltaList.Count);
            
            try
            {
                await _deltaProcessor.ProcessDeltasAsync(userId, deltaList);
                
                // 캐시 무효화 (델타 처리 후 데이터 변경)
                await _cacheManager.InvalidateCacheAsync(userId);

                _logger.LogInformation("[UserStateService] MergeDeltasAsync 완료 - UserId: {UserId}", userId);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "[UserStateService] MergeDeltasAsync 실패 - UserId: {UserId}", userId);
                throw;
            }
        }

        /// <summary>
        /// 사용자 존재 여부 확인
        /// </summary>
        public async Task<bool> UserExistsAsync(string userId)
        {
            try
            {
                return await _dbContext.Users.AnyAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserStateService] UserExistsAsync 실패 - UserId: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 사용자 삭제 (GDPR 준수)
        /// </summary>
        public async Task<bool> DeleteUserAsync(string userId)
        {
            _logger.LogInformation("[UserStateService] DeleteUserAsync 시작 - UserId: {UserId}", userId);
            
            try
            {
                var success = await _saver.DeleteUserAsync(userId);
                
                if (success)
                {
                    // 캐시에서 삭제
                    await _cacheManager.InvalidateCacheAsync(userId);
                }
                
                _logger.LogInformation("[UserStateService] DeleteUserAsync 완료 - UserId: {UserId}, Success: {Success}", 
                    userId, success);
                return success;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "[UserStateService] DeleteUserAsync 실패 - UserId: {UserId}", userId);
                return false;
            }
        }

        #endregion

        #region 추가 편의 메서드들

        /// <summary>
        /// 캐시 상태 확인
        /// </summary>
        public async Task<bool> IsCachedAsync(string userId)
        {
            return await _cacheManager.IsCachedAsync(userId);
        }

        /// <summary>
        /// 캐시 무효화 (관리용)
        /// </summary>
        public async Task InvalidateCacheAsync(string userId)
        {
            await _cacheManager.InvalidateCacheAsync(userId);
        }

        /// <summary>
        /// 캐시 통계 조회 (디버깅/모니터링용)
        /// </summary>
        public async Task<CacheStats> GetCacheStatsAsync(string userId)
        {
            return await _cacheManager.GetCacheStatsAsync(userId);
        }

        /// <summary>
        /// 사용자의 스테이지 기록만 조회 (경량)
        /// </summary>
        public async Task<List<UserStageRecord>> GetUserStageRecordsAsync(string userId)
        {
            return await _loader.LoadUserStageRecordsAsync(userId);
        }

        /// <summary>
        /// 사용자의 업적 진행률만 조회 (경량)
        /// </summary>
        public async Task<UserAchievementProgress> GetUserAchievementProgressAsync(string userId)
        {
            return await _loader.LoadUserAchievementProgressAsync(userId);
        }

        #endregion
    }
} 