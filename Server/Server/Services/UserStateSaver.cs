using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Server.Database;
using Server.Models;
using System;
using System.Threading.Tasks;

namespace Server.Services
{
    /// <summary>
    /// 사용자 상태 저장 전담 서비스
    /// </summary>
    public class UserStateSaver
    {
        private readonly JustClimbDbContext _dbContext;
        private readonly UserStateMapper _mapper;
        private readonly UserStateCacheManager _cacheManager;
        private readonly ILogger<UserStateSaver> _logger;

        public UserStateSaver(
            JustClimbDbContext dbContext,
            UserStateMapper mapper,
            UserStateCacheManager cacheManager,
            ILogger<UserStateSaver> logger)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _cacheManager = cacheManager;
            _logger = logger;
        }

        /// <summary>
        /// 사용자 상태를 데이터베이스에 저장합니다
        /// </summary>
        public async Task<bool> SaveUserStateAsync(string userId, SaveData saveData)
        {
            _logger.LogInformation("[UserStateSaver] 사용자 상태 저장 시작 - UserId: {UserId}", userId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. SaveData를 정규화된 엔티티들로 매핑 및 저장
                await _mapper.MapAndSaveToDbAsync(userId, saveData);

                // 2. 트랜잭션 커밋
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("[UserStateSaver] DB 저장 완료 - UserId: {UserId}", userId);

                // 3. 캐시 갱신 (새로운 데이터로 캐시 업데이트)
                await _cacheManager.RefreshCacheAsync(userId, saveData);

                _logger.LogInformation("[UserStateSaver] 사용자 상태 저장 완료 - UserId: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "[UserStateSaver] 사용자 상태 저장 실패 - UserId: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 사용자 데이터를 완전히 삭제합니다
        /// </summary>
        public async Task<bool> DeleteUserAsync(string userId)
        {
            _logger.LogInformation("[UserStateSaver] 사용자 삭제 시작 - UserId: {UserId}", userId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 1. 연관된 모든 데이터 삭제 (외래키 순서 고려)
                
                // 사용자 업적 진행률 삭제
                var progressRecords = await _dbContext.UserAchievementProgress
                    .Where(p => p.UserId == userId)
                    .ToListAsync();
                _dbContext.UserAchievementProgress.RemoveRange(progressRecords);

                // 사용자 업적 삭제
                var achievementRecords = await _dbContext.UserAchievements
                    .Where(ua => ua.UserId == userId)
                    .ToListAsync();
                _dbContext.UserAchievements.RemoveRange(achievementRecords);

                // 사용자 스테이지 기록 삭제
                var stageRecords = await _dbContext.UserStageRecords
                    .Where(r => r.UserId == userId)
                    .ToListAsync();
                _dbContext.UserStageRecords.RemoveRange(stageRecords);

                // 사용자 아이템 삭제
                var itemRecords = await _dbContext.UserItems
                    .Where(i => i.UserId == userId)
                    .ToListAsync();
                _dbContext.UserItems.RemoveRange(itemRecords);

                // 사용자 기본 정보 삭제
                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user != null)
                {
                    _dbContext.Users.Remove(user);
                }

                // 2. 트랜잭션 커밋
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("[UserStateSaver] DB 삭제 완료 - UserId: {UserId}", userId);

                // 3. 캐시 무효화
                await _cacheManager.InvalidateCacheAsync(userId);

                _logger.LogInformation("[UserStateSaver] 사용자 삭제 완료 - UserId: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "[UserStateSaver] 사용자 삭제 실패 - UserId: {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// 특정 사용자의 캐시만 새로고침합니다
        /// </summary>
        public async Task<bool> RefreshUserCacheAsync(string userId)
        {
            _logger.LogInformation("[UserStateSaver] 사용자 캐시 새로고침 시작 - UserId: {UserId}", userId);

            try
            {
                // 1. 캐시 무효화
                await _cacheManager.InvalidateCacheAsync(userId);

                _logger.LogInformation("[UserStateSaver] 사용자 캐시 새로고침 완료 - UserId: {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[UserStateSaver] 사용자 캐시 새로고침 실패 - UserId: {UserId}", userId);
                return false;
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
                _logger.LogError(ex, "[UserStateSaver] 사용자 존재 확인 실패 - UserId: {UserId}", userId);
                return false;
            }
        }
    }
} 