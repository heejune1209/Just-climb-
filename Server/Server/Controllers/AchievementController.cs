using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // JWT 인증 필요
    public class AchievementController : ControllerBase
    {
        private readonly IAchievementService _achievementService;
        private readonly ILogger<AchievementController> _logger;

        public AchievementController(IAchievementService achievementService, ILogger<AchievementController> logger)
        {
            _achievementService = achievementService;
            _logger = logger;
        }

        /// <summary>
        /// 사용자의 모든 업적 조회 (컬럼 기반 구조)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<UserAchievement>> GetUserAchievements()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user ID");
                }

                var achievements = await _achievementService.GetUserAchievementsAsync(userId);
                return Ok(achievements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all achievements");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 특정 업적 해제 여부 조회
        /// </summary>
        [HttpGet("{achievementId}/unlocked")]
        public async Task<ActionResult<bool>> IsAchievementUnlocked(string achievementId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user ID");
                }

                var isUnlocked = await _achievementService.IsAchievementUnlockedAsync(userId, achievementId);
                return Ok(new { achievementId, isUnlocked });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking achievement {AchievementId}", achievementId);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 특정 업적 보상 수령 여부 조회
        /// </summary>
        [HttpGet("{achievementId}/reward-claimed")]
        public async Task<ActionResult<bool>> IsRewardClaimed(string achievementId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user ID");
                }

                var isClaimed = await _achievementService.IsRewardClaimedAsync(userId, achievementId);
                return Ok(new { achievementId, isRewardClaimed = isClaimed });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking reward claim status for {AchievementId}", achievementId);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 업적 해제 처리
        /// </summary>
        [HttpPost("{achievementId}/unlock")]
        public async Task<ActionResult> UnlockAchievement(string achievementId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user ID");
                }

                var success = await _achievementService.UnlockAchievementAsync(userId, achievementId);
                if (success)
                {
                    return Ok(new { message = "Achievement unlocked successfully", achievementId });
                }
                else
                {
                    return BadRequest(new { error = "Failed to unlock achievement" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking achievement {AchievementId}", achievementId);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 업적 보상 수령 처리
        /// </summary>
        [HttpPost("{achievementId}/claim-reward")]
        public async Task<ActionResult> ClaimReward(string achievementId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user ID");
                }

                var success = await _achievementService.ClaimRewardAsync(userId, achievementId);
                if (success)
                {
                    return Ok(new { message = "Reward claimed successfully", achievementId });
                }
                else
                {
                    return BadRequest(new { error = "Failed to claim reward or already claimed" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error claiming reward for {AchievementId}", achievementId);
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// 사용자 업적 초기화 (새 사용자용)
        /// </summary>
        [HttpPost("initialize")]
        public async Task<ActionResult> InitializeAchievements()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Invalid user ID");
                }

                await _achievementService.InitializeUserAchievementsAsync(userId);
                return Ok(new { message = "Achievements initialized successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing achievements");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// JWT 토큰에서 사용자 ID 추출
        /// </summary>
        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
} 