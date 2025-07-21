using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Server.Database;
using Server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Services
{
    /// <summary>
    /// 사용자 상태 데이터 매핑 전담 서비스
    /// 정규화된 DB 데이터와 SaveData 간의 변환을 담당합니다.
    /// </summary>
    public class UserStateMapper
    {
        private readonly JustClimbDbContext _dbContext;

        public UserStateMapper(JustClimbDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        /// <summary>
        /// 정규화된 DB 데이터를 SaveData로 매핑
        /// </summary>
        public SaveData MapToSaveData(
            User user, 
            List<UserStageRecord> stageRecords, 
            UserAchievementProgress progressRecord, 
            Dictionary<string, bool> achievementUnlocked,
            Dictionary<string, bool> achievementRewards)
        {
            return new SaveData
            {
                gold = user.Gold,
                gems = user.Gems,
                selectedCharacter = user.SelectedCharacter,
                tutorialDisplayed = user.TutorialDisplayed,
                
                // 아이템 데이터 매핑
                items = user.Items?.Select(MapToInventoryItem).ToList() ?? new List<InventoryItemDto>(),
                
                // 스테이지 데이터 매핑 (클라이언트 캐싱용 배열 구조)
                stageClears = BuildStageClears(stageRecords),
                stageFlagPositions = BuildStageFlagPositions(stageRecords),
                bestGemRewards = BuildBestGemRewards(stageRecords),
                bestClearTimes = BuildBestClearTimes(stageRecords),
                bestDeathCounts = BuildBestDeathCounts(stageRecords),
                currentPlayTimes = BuildCurrentPlayTimes(stageRecords),
                currentDeathCounts = BuildCurrentDeathCounts(stageRecords),
                
                // 업적 데이터 매핑
                achievementUnlocked = achievementUnlocked ?? new Dictionary<string, bool>(),
                achievementRewards = achievementRewards ?? new Dictionary<string, bool>(),
                achievementProgress = MapToAchievementProgress(progressRecord),
                
                version = 5 // 현재 버전
            };
        }

        /// <summary>
        /// UserItem을 InventoryItemDto로 매핑
        /// </summary>
        private InventoryItemDto MapToInventoryItem(UserItem userItem)
        {
            return new InventoryItemDto
            {
                itemId = userItem.ItemId,
                count = userItem.Count
            };
        }

        /// <summary>
        /// UserAchievementProgress를 AchievementProgressDto로 매핑
        /// </summary>
        private AchievementProgressDto MapToAchievementProgress(UserAchievementProgress progressRecord)
        {
            if (progressRecord == null)
                return new AchievementProgressDto();

            return new AchievementProgressDto
            {
                stagesCompleted = progressRecord.StagesCompleted,
                perfectClears = progressRecord.PerfectClears,
                speedClears = progressRecord.SpeedClears,
                chapter1PerfectStages = progressRecord.Chapter1PerfectStages,
                itemsPurchased = progressRecord.ItemsPurchased,
                unlockedCharacters = DeserializeJsonList(progressRecord.UnlockedCharactersJson),
                itemTypesUsed = DeserializeJsonList(progressRecord.ItemTypesUsedJson),
                deathsInCurrentStage = progressRecord.DeathsInCurrentStage,
                usedItemInCurrentStage = progressRecord.UsedItemInCurrentStage,
                totalDeaths = progressRecord.TotalDeaths,
                totalGemsCollected = progressRecord.TotalGemsCollected
            };
        }

        #region 스테이지 데이터 매핑 메서드들

        /// <summary>
        /// 스테이지 클리어 상태 리스트 생성
        /// </summary>
        private List<bool> BuildStageClears(List<UserStageRecord> stageRecords)
        {
            const int maxStages = 50; // 최대 스테이지 수
            var clears = new List<bool>();
            
            // 스테이지별로 순서대로 초기화
            for (int i = 1; i <= maxStages; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                clears.Add(record?.IsCleared ?? false);
            }
            
            return clears;
        }

        /// <summary>
        /// 스테이지 깃발 위치 리스트 생성
        /// </summary>
        private List<SerializableVector3Dto> BuildStageFlagPositions(List<UserStageRecord> stageRecords)
        {
            const int maxStages = 50;
            var positions = new List<SerializableVector3Dto>();
            
            // 스테이지별로 순서대로 초기화
            for (int i = 1; i <= maxStages; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                if (record != null && record.FlagX.HasValue && record.FlagY.HasValue && record.FlagZ.HasValue)
                {
                    positions.Add(new SerializableVector3Dto(record.FlagX.Value, record.FlagY.Value, record.FlagZ.Value));
                }
                else
                {
                    positions.Add(new SerializableVector3Dto(0, 0, 0));
                }
            }
            
            return positions;
        }

        /// <summary>
        /// 최고 보석 보상 리스트 생성
        /// </summary>
        private List<int> BuildBestGemRewards(List<UserStageRecord> stageRecords)
        {
            const int maxStages = 50;
            var rewards = new List<int>();
            
            // 스테이지별로 순서대로 초기화
            for (int i = 1; i <= maxStages; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                rewards.Add(record?.BestGemCount ?? 0);
            }
            
            return rewards;
        }

        /// <summary>
        /// 최단 클리어 시간 리스트 생성
        /// </summary>
        private List<float> BuildBestClearTimes(List<UserStageRecord> stageRecords)
        {
            const int maxStages = 50;
            var times = new List<float>();
            
            // 스테이지별로 순서대로 초기화
            for (int i = 1; i <= maxStages; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                times.Add(record?.BestClearTime ?? float.MaxValue);
            }
            
            return times;
        }

        /// <summary>
        /// 최소 사망 횟수 리스트 생성
        /// </summary>
        private List<int> BuildBestDeathCounts(List<UserStageRecord> stageRecords)
        {
            const int maxStages = 50;
            var deaths = new List<int>();
            
            // 스테이지별로 순서대로 초기화
            for (int i = 1; i <= maxStages; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                deaths.Add(record?.BestDeathCount ?? int.MaxValue);
            }
            
            return deaths;
        }

        /// <summary>
        /// 현재 플레이 시간 리스트 생성
        /// </summary>
        private List<float> BuildCurrentPlayTimes(List<UserStageRecord> stageRecords)
        {
            const int maxStages = 50;
            var times = new List<float>();
            
            // 스테이지별로 순서대로 초기화
            for (int i = 1; i <= maxStages; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                times.Add(record?.CurrentPlayTime ?? 0f);
            }
            
            return times;
        }

        /// <summary>
        /// 현재 사망 횟수 리스트 생성
        /// </summary>
        private List<int> BuildCurrentDeathCounts(List<UserStageRecord> stageRecords)
        {
            const int maxStages = 50;
            var deaths = new List<int>();
            
            // 스테이지별로 순서대로 초기화
            for (int i = 1; i <= maxStages; i++)
            {
                var record = stageRecords.FirstOrDefault(r => r.StageNumber == i);
                deaths.Add(record?.CurrentDeathCount ?? 0);
            }
            
            return deaths;
        }

        #endregion

        #region JSON 직렬화/역직렬화 유틸리티

        /// <summary>
        /// JSON 문자열을 리스트로 역직렬화
        /// </summary>
        private List<T> DeserializeJsonList<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new List<T>();
            
            try
            {
                return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
            }
            catch (JsonException)
            {
                return new List<T>();
            }
        }

        /// <summary>
        /// 문자열 리스트 전용 역직렬화 (타입 추론용)
        /// </summary>
        private List<string> DeserializeJsonList(string json)
        {
            return DeserializeJsonList<string>(json);
        }

        /// <summary>
        /// JSON 문자열을 객체로 역직렬화
        /// </summary>
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

        /// <summary>
        /// 객체를 JSON 문자열로 직렬화
        /// </summary>
        public string SerializeToJson<T>(T obj)
        {
            if (obj == null)
                return string.Empty;
            
            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch (JsonException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// SaveData를 정규화된 DB 테이블에 매핑하고 저장
        /// </summary>
        public async Task MapAndSaveToDbAsync(string userId, SaveData saveData)
        {
            // 사용자 기본 정보 업데이트
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                user = new User
                {
                    Id = userId,
                    SteamDisplayName = "Unknown",
                    Gold = saveData.gold,
                    Gems = saveData.gems,
                    SelectedCharacter = saveData.selectedCharacter,
                    TutorialDisplayed = saveData.tutorialDisplayed,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _dbContext.Users.Add(user);
            }
            else
            {
                user.Gold = saveData.gold;
                user.Gems = saveData.gems;
                user.SelectedCharacter = saveData.selectedCharacter;
                user.TutorialDisplayed = saveData.tutorialDisplayed;
                user.UpdatedAt = DateTime.UtcNow;
            }

            // 사용자 아이템 업데이트
            var existingItems = await _dbContext.UserItems
                .Where(ui => ui.UserId == userId)
                .ToListAsync();
            
            _dbContext.UserItems.RemoveRange(existingItems);
            
            if (saveData.items != null)
            {
                foreach (var item in saveData.items)
                {
                    _dbContext.UserItems.Add(new UserItem
                    {
                        UserId = userId,
                        ItemId = item.itemId,
                        Count = item.count
                    });
                }
            }

            // 스테이지 기록 업데이트 (완전히 재작성)
            for (int i = 0; i < saveData.stageClears.Count; i++)
            {
                if (saveData.stageClears[i])
                {
                    var existingRecord = await _dbContext.UserStageRecords
                        .FirstOrDefaultAsync(r => r.UserId == userId && r.StageNumber == i + 1);
                    
                    if (existingRecord == null)
                    {
                        var flagPos = saveData.stageFlagPositions.Count > i ? saveData.stageFlagPositions[i] : null;
                        
                        _dbContext.UserStageRecords.Add(new UserStageRecord
                        {
                            UserId = userId,
                            StageNumber = i + 1,
                            IsCleared = true,
                            BestClearTime = saveData.bestClearTimes.Count > i ? saveData.bestClearTimes[i] : 0f,
                            BestDeathCount = saveData.bestDeathCounts.Count > i ? saveData.bestDeathCounts[i] : 0,
                            BestGemCount = saveData.bestGemRewards.Count > i ? saveData.bestGemRewards[i] : 0,
                            CurrentPlayTime = saveData.currentPlayTimes.Count > i ? saveData.currentPlayTimes[i] : 0f,
                            CurrentDeathCount = saveData.currentDeathCounts.Count > i ? saveData.currentDeathCounts[i] : 0,
                            FlagX = flagPos?.x,
                            FlagY = flagPos?.y,
                            FlagZ = flagPos?.z,
                            DisplayName = "Player",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });
                    }
                    else
                    {
                        existingRecord.IsCleared = true;
                        
                        if (saveData.bestClearTimes.Count > i)
                            existingRecord.BestClearTime = saveData.bestClearTimes[i];
                        if (saveData.bestDeathCounts.Count > i)
                            existingRecord.BestDeathCount = saveData.bestDeathCounts[i];
                        if (saveData.bestGemRewards.Count > i)
                            existingRecord.BestGemCount = saveData.bestGemRewards[i];
                        if (saveData.currentPlayTimes.Count > i)
                            existingRecord.CurrentPlayTime = saveData.currentPlayTimes[i];
                        if (saveData.currentDeathCounts.Count > i)
                            existingRecord.CurrentDeathCount = saveData.currentDeathCounts[i];
                        
                        if (saveData.stageFlagPositions.Count > i)
                        {
                            var flagPos = saveData.stageFlagPositions[i];
                            existingRecord.FlagX = flagPos.x;
                            existingRecord.FlagY = flagPos.y;
                            existingRecord.FlagZ = flagPos.z;
                        }
                        
                        existingRecord.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        #endregion
    }
} 