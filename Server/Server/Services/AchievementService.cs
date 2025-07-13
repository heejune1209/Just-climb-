using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Server.Services
{
    public interface IAchievementService
    {
        Task<bool> UnlockAchievementAsync(string userId, string achievementCode);
        Task<bool> ClaimRewardAsync(string userId, string achievementCode);
        Task<bool> IsAchievementUnlockedAsync(string userId, string achievementCode);
        Task<bool> IsRewardClaimedAsync(string userId, string achievementCode);
        Task<List<Achievement>> GetAllAchievementsAsync();
        Task<List<UserAchievement>> GetUserAchievementsAsync(string userId);
        Task<Dictionary<string, bool>> GetUserAchievementUnlockedMapAsync(string userId);
        Task<Dictionary<string, bool>> GetUserAchievementRewardMapAsync(string userId);
        Task<bool> ProcessAchievementDeltaAsync(string userId, string key, string value);
        Task<bool> InitializeUserAchievementsAsync(string userId);
    }

    public class AchievementService : IAchievementService
    {
        private readonly JustClimbDbContext _dbContext;
        private readonly ILogger<AchievementService> _logger;

        public AchievementService(JustClimbDbContext dbContext, ILogger<AchievementService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// 업적 해제
        /// </summary>
        public async Task<bool> UnlockAchievementAsync(string userId, string achievementCode)
        {
            try
            {
                // 업적 정의 조회
                var achievement = await _dbContext.Achievements
                    .FirstOrDefaultAsync(a => a.Code == achievementCode && a.IsActive);

                if (achievement == null)
                {
                    _logger.LogWarning($"[AchievementService] 업적을 찾을 수 없음: {achievementCode}");
                    return false;
                }

                // 이미 해제된 업적인지 확인
                var existingUserAchievement = await _dbContext.UserAchievements
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievement.AchievementId);

                if (existingUserAchievement != null)
                {
                    _logger.LogInformation($"[AchievementService] 이미 해제된 업적: {achievementCode} for user {userId}");
                    return true; // 이미 해제된 상태이므로 성공으로 처리
                }

                // 새로운 업적 해제 기록 생성
                var userAchievement = new UserAchievement
                {
                    UserId = userId,
                    AchievementId = achievement.AchievementId,
                    UnlockedAt = DateTime.UtcNow,
                    ClaimedAt = null // 보상은 별도로 수령
                };

                _dbContext.UserAchievements.Add(userAchievement);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"[AchievementService] 업적 해제 완료: {achievementCode} for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AchievementService] 업적 해제 실패: {achievementCode} for user {userId}");
                return false;
            }
        }

        /// <summary>
        /// 보상 수령
        /// </summary>
        public async Task<bool> ClaimRewardAsync(string userId, string achievementCode)
        {
            try
            {
                // 업적과 사용자 상태 조회 (JOIN)
                var userAchievement = await _dbContext.UserAchievements
                    .Include(ua => ua.Achievement)
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && 
                                              ua.Achievement.Code == achievementCode && 
                                              ua.Achievement.IsActive);

                if (userAchievement == null)
                {
                    _logger.LogWarning($"[AchievementService] 해제되지 않은 업적의 보상 요청: {achievementCode} for user {userId}");
                    return false;
                }

                if (userAchievement.ClaimedAt != null)
                {
                    _logger.LogInformation($"[AchievementService] 이미 수령된 보상: {achievementCode} for user {userId}");
                    return true; // 이미 수령된 상태이므로 성공으로 처리
                }

                // 보상 수령 처리
                userAchievement.ClaimedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"[AchievementService] 보상 수령 완료: {achievementCode} for user {userId} at {userAchievement.ClaimedAt}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AchievementService] 보상 수령 실패: {achievementCode} for user {userId}");
                return false;
            }
        }

        /// <summary>
        /// 업적 해제 여부 확인
        /// </summary>
        public async Task<bool> IsAchievementUnlockedAsync(string userId, string achievementCode)
        {
            try
            {
                var exists = await _dbContext.UserAchievements
                    .AnyAsync(ua => ua.UserId == userId && 
                                   ua.Achievement.Code == achievementCode && 
                                   ua.Achievement.IsActive);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AchievementService] 업적 해제 여부 확인 실패: {achievementCode} for user {userId}");
                return false;
            }
        }

        /// <summary>
        /// 보상 수령 여부 확인
        /// </summary>
        public async Task<bool> IsRewardClaimedAsync(string userId, string achievementCode)
        {
            try
            {
                var userAchievement = await _dbContext.UserAchievements
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && 
                                              ua.Achievement.Code == achievementCode && 
                                              ua.Achievement.IsActive);

                return userAchievement?.ClaimedAt != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AchievementService] 보상 수령 여부 확인 실패: {achievementCode} for user {userId}");
                return false;
            }
        }

        /// <summary>
        /// 모든 활성 업적 조회
        /// </summary>
        public async Task<List<Achievement>> GetAllAchievementsAsync()
        {
            try
            {
                return await _dbContext.Achievements
                    .Where(a => a.IsActive)
                    .OrderBy(a => a.Category)
                    .ThenBy(a => a.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AchievementService] 모든 업적 조회 실패");
                return new List<Achievement>();
            }
        }

        /// <summary>
        /// 사용자의 모든 업적 상태 조회
        /// </summary>
        public async Task<List<UserAchievement>> GetUserAchievementsAsync(string userId)
        {
            try
            {
                return await _dbContext.UserAchievements
                    .Include(ua => ua.Achievement)
                    .Where(ua => ua.UserId == userId && ua.Achievement.IsActive)
                    .OrderBy(ua => ua.Achievement.Category)
                    .ThenBy(ua => ua.Achievement.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AchievementService] 사용자 업적 조회 실패: {userId}");
                return new List<UserAchievement>();
            }
        }

        /// <summary>
        /// 사용자 업적 해제 상태 맵 (클라이언트 호환성)
        /// </summary>
        public async Task<Dictionary<string, bool>> GetUserAchievementUnlockedMapAsync(string userId)
        {
            try
            {
                var userAchievements = await _dbContext.UserAchievements
                    .Include(ua => ua.Achievement)
                    .Where(ua => ua.UserId == userId && ua.Achievement.IsActive)
                    .Select(ua => ua.Achievement.Code)
                    .ToListAsync();

                return userAchievements.ToDictionary(code => code, _ => true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AchievementService] 사용자 업적 해제 맵 조회 실패: {userId}");
                return new Dictionary<string, bool>();
            }
        }

        /// <summary>
        /// 사용자 업적 보상 수령 상태 맵 (클라이언트 호환성)
        /// </summary>
        public async Task<Dictionary<string, bool>> GetUserAchievementRewardMapAsync(string userId)
        {
            try
            {
                var claimedAchievements = await _dbContext.UserAchievements
                    .Include(ua => ua.Achievement)
                    .Where(ua => ua.UserId == userId && 
                                ua.Achievement.IsActive && 
                                ua.ClaimedAt != null)
                    .Select(ua => ua.Achievement.Code)
                    .ToListAsync();

                return claimedAchievements.ToDictionary(code => code, _ => true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AchievementService] 사용자 업적 보상 맵 조회 실패: {userId}");
                return new Dictionary<string, bool>();
            }
        }

        /// <summary>
        /// 클라이언트 델타 처리 (기존 호환성 유지)
        /// </summary>
        public async Task<bool> ProcessAchievementDeltaAsync(string userId, string key, string value)
        {
            try
            {
                if (key.StartsWith("achievementUnlocked_"))
                {
                    var achievementCode = key.Replace("achievementUnlocked_", "");
                    bool isUnlocked = bool.Parse(value);

                    if (isUnlocked)
                    {
                        return await UnlockAchievementAsync(userId, achievementCode);
                    }
                }
                else if (key == "achievementRewards")
                {
                    // Dictionary<string, bool> 형태의 JSON 파싱
                    var rewardMap = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, bool>>(value);
                    if (rewardMap != null)
                    {
                        foreach (var kvp in rewardMap.Where(kvp => kvp.Value))
                        {
                            await ClaimRewardAsync(userId, kvp.Key);
                        }
                    }
                    return true;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AchievementService] 델타 처리 실패: {key} = {value} for user {userId}");
                return false;
            }
        }

        /// <summary>
        /// 사용자 업적 초기화 (신규 사용자용)
        /// </summary>
        public async Task<bool> InitializeUserAchievementsAsync(string userId)
        {
            try
            {
                _logger.LogInformation($"[AchievementService] 사용자 업적 초기화: {userId}");

                // 정규화된 데이터베이스 구조에서는 특별한 초기화가 필요하지 않음
                // 업적은 achievements 테이블에 이미 시드되어 있고,
                // user_achievements는 실제 업적 달성 시 생성됨

                // 기존 시스템과의 호환성을 위해 성공으로 반환
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[AchievementService] 사용자 업적 초기화 실패: {userId}");
                return false;
            }
        }
    }
} 