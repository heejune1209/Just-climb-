using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Server.Database;
using Server.Models;
using Server.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Services
{
    /// <summary>
    /// 델타 이벤트 처리 전담 서비스
    /// </summary>
    public class DeltaProcessor
    {
        private readonly JustClimbDbContext _dbContext;
        private readonly ConflictResolver _conflictResolver;
        private readonly ILogger<DeltaProcessor> _logger;

        public DeltaProcessor(
            JustClimbDbContext dbContext,
            ConflictResolver conflictResolver,
            ILogger<DeltaProcessor> logger)
        {
            _dbContext = dbContext;
            _conflictResolver = conflictResolver;
            _logger = logger;
        }

        /// <summary>
        /// 여러 델타 이벤트들을 처리합니다
        /// </summary>
        public async Task ProcessDeltasAsync(string userId, List<DeltaEventDto> deltas)
        {
            _logger.LogInformation("[DeltaProcessor] 델타 처리 시작 - UserId: {UserId}, Count: {Count}", userId, deltas.Count);

            foreach (var delta in deltas)
            {
                await ProcessSingleDeltaAsync(userId, delta);
            }

            _logger.LogInformation("[DeltaProcessor] 델타 처리 완료 - UserId: {UserId}", userId);
        }

        /// <summary>
        /// 개별 델타를 적절한 테이블에 처리
        /// </summary>
        private async Task ProcessSingleDeltaAsync(string userId, DeltaEventDto delta)
        {
            try
            {
                // 기본 필드들은 ConflictResolver 사용
                if (IsBasicUserField(delta.Key))
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null)
                    {
                        _conflictResolver.Resolve(user, delta);
                        _logger.LogDebug("[DeltaProcessor] 기본 필드 처리 완료 - Key: {Key}", delta.Key);
                    }
                    return;
                }

                // 스테이지 관련 델타 처리
                if (IsStageRelatedDelta(delta.Key))
                {
                    await ProcessStageRelatedDelta(userId, delta);
                    return;
                }

                // 업적 관련 델타 처리
                if (IsAchievementRelatedDelta(delta.Key))
                {
                    await ProcessAchievementRelatedDelta(userId, delta);
                    return;
                }

                // 아이템 관련 델타 처리
                if (delta.Key == "items")
                {
                    await ProcessItemsDelta(userId, delta);
                    return;
                }

                _logger.LogWarning("[DeltaProcessor] 처리되지 않은 델타 키: {Key}", delta.Key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DeltaProcessor] 델타 처리 중 오류 - UserId: {UserId}, Key: {Key}", userId, delta.Key);
                throw;
            }
        }

        /// <summary>
        /// 기본 사용자 필드인지 확인
        /// </summary>
        private bool IsBasicUserField(string key)
        {
            return key == "gold" || key == "gems" || key == "selectedCharacter" || key == "tutorialDisplayed";
        }

        /// <summary>
        /// 스테이지 관련 델타인지 확인
        /// </summary>
        private bool IsStageRelatedDelta(string key)
        {
            return key.StartsWith("currentPlayTimes_") || key.StartsWith("currentDeathCounts_") ||
                   key.StartsWith("bestClearTimes_") || key.StartsWith("bestDeathCounts_") ||
                   key.StartsWith("bestGemRewards_") || key.StartsWith("stageFlagPositions_");
        }

        /// <summary>
        /// 업적 관련 델타인지 확인
        /// </summary>
        private bool IsAchievementRelatedDelta(string key)
        {
            return key.StartsWith("achievementUnlocked_") || key.StartsWith("achievementReward_") ||
                   key == "achievementProgress";
        }

        /// <summary>
        /// 스테이지 관련 델타 처리
        /// </summary>
        private async Task ProcessStageRelatedDelta(string userId, DeltaEventDto delta)
        {
            // 스테이지 인덱스 추출
            var parts = delta.Key.Split('_');
            if (parts.Length < 2 || !int.TryParse(parts[1], out int stageIndex))
            {
                _logger.LogWarning("[DeltaProcessor] 잘못된 스테이지 델타 형식: {Key}", delta.Key);
                return;
            }

            // UserStageRecord 찾기 또는 생성
            var record = await _dbContext.UserStageRecords
                .FirstOrDefaultAsync(r => r.UserId == userId && r.StageNumber == stageIndex);

            if (record == null)
            {
                record = new UserStageRecord 
                { 
                    UserId = userId, 
                    StageNumber = stageIndex,
                    DisplayName = "Player",
                    CreatedAt = DateTime.UtcNow
                };
                _dbContext.UserStageRecords.Add(record);
            }

            // 델타 타입에 따라 값 업데이트
            if (delta.Key.StartsWith("currentPlayTimes_"))
                record.CurrentPlayTime = ParseFloat(delta.Value);
            else if (delta.Key.StartsWith("currentDeathCounts_"))
                record.CurrentDeathCount = ParseInt(delta.Value);
            else if (delta.Key.StartsWith("bestClearTimes_"))
                record.BestClearTime = ParseFloat(delta.Value);
            else if (delta.Key.StartsWith("bestDeathCounts_"))
                record.BestDeathCount = ParseInt(delta.Value);
            else if (delta.Key.StartsWith("bestGemRewards_"))
                record.BestGemCount = ParseInt(delta.Value);
            else if (delta.Key.StartsWith("stageFlagPositions_"))
            {
                // JSON으로 받은 깃발 위치를 개별 좌표로 분리
                var flagPos = DeserializeJson<SerializableVector3Dto>(delta.Value);
                if (flagPos != null)
                {
                    record.FlagX = flagPos.x;
                    record.FlagY = flagPos.y;
                    record.FlagZ = flagPos.z;
                }
            }

            record.UpdatedAt = DateTime.UtcNow;

            _logger.LogDebug("[DeltaProcessor] 스테이지 델타 처리 완료 - Key: {Key}, Stage: {Stage}", delta.Key, stageIndex);
        }

        /// <summary>
        /// 업적 관련 델타 처리
        /// </summary>
        private async Task ProcessAchievementRelatedDelta(string userId, DeltaEventDto delta)
        {
            if (delta.Key.StartsWith("achievementUnlocked_"))
            {
                // 업적 해제 상태 처리
                var achievementIdString = delta.Key.Substring("achievementUnlocked_".Length);
                if (!int.TryParse(achievementIdString, out int achievementId))
                {
                    _logger.LogWarning("[DeltaProcessor] 잘못된 업적 ID 형식: {AchievementId}", achievementIdString);
                    return;
                }
                
                var isUnlocked = ParseBool(delta.Value);

                var record = await _dbContext.UserAchievements
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);

                if (record == null && isUnlocked)
                {
                    record = new UserAchievement
                    {
                        UserId = userId,
                        AchievementId = achievementId,
                        UnlockedAt = DateTime.UtcNow
                    };
                    _dbContext.UserAchievements.Add(record);
                }
                // 이미 해제된 업적은 다시 처리하지 않음
            }
            else if (delta.Key.StartsWith("achievementReward_"))
            {
                // 업적 보상 수령 상태 처리
                var achievementIdString = delta.Key.Substring("achievementReward_".Length);
                if (!int.TryParse(achievementIdString, out int achievementId))
                {
                    _logger.LogWarning("[DeltaProcessor] 잘못된 업적 ID 형식: {AchievementId}", achievementIdString);
                    return;
                }
                
                var isRewarded = ParseBool(delta.Value);

                var record = await _dbContext.UserAchievements
                    .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);

                if (record != null && isRewarded && record.ClaimedAt == null)
                {
                    record.ClaimedAt = DateTime.UtcNow;
                }
            }
            else if (delta.Key == "achievementProgress")
            {
                // 업적 진행률 처리
                var progress = await _dbContext.UserAchievementProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (progress == null)
                {
                    progress = new UserAchievementProgress
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.UserAchievementProgress.Add(progress);
                }

                // JSON을 파싱해서 개별 필드에 할당
                var progressDto = DeserializeJson<AchievementProgressDto>(delta.Value);
                if (progressDto != null)
                {
                    progress.StagesCompleted = progressDto.stagesCompleted;
                    progress.PerfectClears = progressDto.perfectClears;
                    progress.SpeedClears = progressDto.speedClears;
                    progress.Chapter1PerfectStages = progressDto.chapter1PerfectStages;
                    progress.ItemsPurchased = progressDto.itemsPurchased;
                    progress.UnlockedCharactersJson = SerializeToJson(progressDto.unlockedCharacters);
                    progress.ItemTypesUsedJson = SerializeToJson(progressDto.itemTypesUsed);
                    progress.DeathsInCurrentStage = progressDto.deathsInCurrentStage;
                    progress.UsedItemInCurrentStage = progressDto.usedItemInCurrentStage;
                    progress.TotalDeaths = progressDto.totalDeaths;
                    progress.TotalGemsCollected = progressDto.totalGemsCollected;
                }
                progress.UpdatedAt = DateTime.UtcNow;
            }

            _logger.LogDebug("[DeltaProcessor] 업적 델타 처리 완료 - Key: {Key}", delta.Key);
        }

        /// <summary>
        /// 아이템 델타 처리
        /// </summary>
        private async Task ProcessItemsDelta(string userId, DeltaEventDto delta)
        {
            // 기존 아이템 삭제
            var existingItems = await _dbContext.UserItems
                .Where(ui => ui.UserId == userId)
                .ToListAsync();
            
            _dbContext.UserItems.RemoveRange(existingItems);

            // 새로운 아이템들 추가 (JSON 파싱)
            var items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<InventoryItemDto>>(delta.Value);
            if (items != null)
            {
                foreach (var item in items)
                {
                    var userItem = new UserItem
                    {
                        UserId = userId,
                        ItemId = item.itemId,
                        Count = item.count
                    };
                    _dbContext.UserItems.Add(userItem);
                }
            }

            _logger.LogDebug("[DeltaProcessor] 아이템 델타 처리 완료 - Count: {Count}", items?.Count ?? 0);
        }

        // 파싱 헬퍼 메서드들
        private int ParseInt(string value, int defaultValue = 0)
        {
            return int.TryParse(value, out int result) ? result : defaultValue;
        }

        private float ParseFloat(string value, float defaultValue = 0f)
        {
            return float.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out float result) ? result : defaultValue;
        }

        private bool ParseBool(string value, bool defaultValue = false)
        {
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        private T DeserializeJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return default(T);

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (JsonException)
            {
                return default(T);
            }
        }

        private string SerializeToJson<T>(T obj)
        {
            if (obj == null)
                return "[]";
            
            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch (JsonException)
            {
                return "[]";
            }
        }
    }
} 