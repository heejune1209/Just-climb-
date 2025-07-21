using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using JustClimb.Data;

namespace JustClimb.Services
{
    /// <summary>
    /// 업적 조건 체크 및 달성 로직 전담 서비스
    /// </summary>
    public class AchievementEventHandler
    {
        private readonly AchievementProgressTracker _progressTracker;

        // 업적 달성 이벤트
        public event Action<string> OnAchievementUnlocked;

        [Inject]
        public AchievementEventHandler(AchievementProgressTracker progressTracker)
        {
            _progressTracker = progressTracker;
        }

        /// <summary>
        /// 스테이지 클리어 시 업적 체크
        /// </summary>
        public void HandleStageCleared(int stageIndex, float clearTime, int deathCount, int gemsCollected, int totalGems)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log($"[AchievementEventHandler] OnStageCleared 호출 - Stage: {stageIndex}, Time: {clearTime}, Deaths: {deathCount}");
#endif

            // 진행률 업데이트
            _progressTracker.UpdateStageProgress(stageIndex, clearTime, deathCount, gemsCollected, totalGems);

            var progress = _progressTracker.GetCurrentProgress();
            if (progress == null) return;

            // 개별 업적 체크
            CheckFirstStageAchievement(stageIndex);
            CheckPerfectClearAchievements(stageIndex, deathCount, gemsCollected, totalGems, progress);
            CheckSpeedRunAchievements(clearTime, progress);
            CheckStageCountAchievements(progress);
            CheckChapter1Achievements(progress);
        }

        /// <summary>
        /// 첫 스테이지 클리어 업적 체크
        /// </summary>
        private void CheckFirstStageAchievement(int stageIndex)
        {
            if (stageIndex == 1)
            {
                TriggerAchievement(AchievementIDs.NOVICE_CLIMBER);
            }
        }

        /// <summary>
        /// Perfect 클리어 관련 업적들 체크
        /// </summary>
        private void CheckPerfectClearAchievements(int stageIndex, int deathCount, int gemsCollected, int totalGems, AchievementProgressDto progress)
        {
            bool isPerfectClear = deathCount == 0 && gemsCollected == totalGems;
            
            if (isPerfectClear)
            {
                // 첫 번째 Perfect 클리어
                if (progress.perfectClears == 1)
                {
                    TriggerAchievement(AchievementIDs.PERFECTIONIST);
                }

                // 5번의 Perfect 클리어
                if (progress.perfectClears >= 5)
                {
                    TriggerAchievement(AchievementIDs.FLAWLESS_CLIMBER);
                }
            }
        }

        /// <summary>
        /// 스피드런 관련 업적들 체크
        /// </summary>
        private void CheckSpeedRunAchievements(float clearTime, AchievementProgressDto progress)
        {
            // Speed Clear (30초 이하)
            if (clearTime <= 30f)
            {
                if (progress.speedClears == 1)
                {
                    TriggerAchievement(AchievementIDs.SPEED_DEMON);
                }

                if (progress.speedClears >= 3)
                {
                    TriggerAchievement(AchievementIDs.LIGHTNING_FAST);
                }
            }
        }

        /// <summary>
        /// 스테이지 클리어 수 관련 업적들 체크
        /// </summary>
        private void CheckStageCountAchievements(AchievementProgressDto progress)
        {
            if (progress.stagesCompleted >= 5)
            {
                TriggerAchievement(AchievementIDs.VETERAN_CLIMBER);
            }

            if (progress.stagesCompleted >= 10)
            {
                TriggerAchievement(AchievementIDs.EXPERIENCED_CLIMBER);
            }

            if (progress.stagesCompleted >= 20)
            {
                TriggerAchievement(AchievementIDs.MASTER_CLIMBER);
            }
        }

        /// <summary>
        /// Chapter 1 관련 업적들 체크
        /// </summary>
        private void CheckChapter1Achievements(AchievementProgressDto progress)
        {
            // Chapter 1의 모든 스테이지를 Perfect로 클리어했는지 확인 (1, 2, 3, 4)
            if (progress.chapter1PerfectStages.Count >= 4)
            {
                bool hasAllChapter1Stages = true;
                for (int i = 1; i <= 4; i++)
                {
                    if (!progress.chapter1PerfectStages.Contains(i))
                    {
                        hasAllChapter1Stages = false;
                        break;
                    }
                }

                if (hasAllChapter1Stages)
                {
                    TriggerAchievement(AchievementIDs.CHAPTER_1_MASTER);
                }
            }
        }

        /// <summary>
        /// 아이템 사용 시 업적 체크
        /// </summary>
        public void HandleItemUsed(string itemType)
        {
            _progressTracker.OnItemUsed(itemType);

            var progress = _progressTracker.GetCurrentProgress();
            if (progress == null) return;

            // 아이템 종류별 업적 체크
            CheckItemTypeAchievements(progress);
        }

        /// <summary>
        /// 아이템 종류별 업적 체크
        /// </summary>
        private void CheckItemTypeAchievements(AchievementProgressDto progress)
        {
            // 3가지 이상의 서로 다른 아이템 타입 사용
            if (progress.itemTypesUsed.Count >= 3)
            {
                TriggerAchievement(AchievementIDs.VERSATILE_CLIMBER);
            }

            // 모든 아이템 타입 사용 (예: 5종류)
            if (progress.itemTypesUsed.Count >= 5)
            {
                TriggerAchievement(AchievementIDs.ITEM_COLLECTOR);
            }
        }

        /// <summary>
        /// 아이템 구매 시 업적 체크
        /// </summary>
        public void HandleItemPurchased(string itemName)
        {
            _progressTracker.OnItemPurchased(itemName);

            var progress = _progressTracker.GetCurrentProgress();
            if (progress == null) return;

            CheckPurchaseAchievements(progress);
        }

        /// <summary>
        /// 구매 관련 업적 체크
        /// </summary>
        private void CheckPurchaseAchievements(AchievementProgressDto progress)
        {
            // 첫 구매
            if (progress.itemsPurchased == 1)
            {
                TriggerAchievement(AchievementIDs.FIRST_PURCHASE);
            }

            // 10번 구매
            if (progress.itemsPurchased >= 10)
            {
                TriggerAchievement(AchievementIDs.BIG_SPENDER);
            }
        }

        /// <summary>
        /// 플레이어 사망 시 업적 체크
        /// </summary>
        public void HandlePlayerDeath()
        {
            _progressTracker.OnPlayerDeath();

            var progress = _progressTracker.GetCurrentProgress();
            if (progress == null) return;

            // 사망 관련 업적 체크 (예: 총 100번 사망)
            if (progress.totalDeaths >= 100)
            {
                TriggerAchievement(AchievementIDs.PERSISTENT_CLIMBER);
            }
        }

        /// <summary>
        /// 캐릭터 해제 시 업적 체크
        /// </summary>
        public void HandleCharacterUnlocked(int characterId)
        {
            _progressTracker.OnCharacterUnlocked(characterId);

            var progress = _progressTracker.GetCurrentProgress();
            if (progress == null) return;

            CheckCharacterAchievements(progress);
        }

        /// <summary>
        /// 캐릭터 관련 업적 체크
        /// </summary>
        private void CheckCharacterAchievements(AchievementProgressDto progress)
        {
            // 첫 캐릭터 해제
            if (progress.unlockedCharacters.Count == 1)
            {
                TriggerAchievement(AchievementIDs.CHARACTER_COLLECTOR);
            }

            // 모든 캐릭터 해제 (예: 5명)
            if (progress.unlockedCharacters.Count >= 5)
            {
                TriggerAchievement(AchievementIDs.FULL_ROSTER);
            }
        }

        /// <summary>
        /// 업적 달성 트리거
        /// </summary>
        private void TriggerAchievement(string achievementID)
        {
            // 이미 달성된 업적인지 확인
            if (_progressTracker.IsAchievementUnlocked(achievementID))
            {
                return;
            }

            // 업적 달성 알림
            OnAchievementUnlocked?.Invoke(achievementID);
        }
    }
} 