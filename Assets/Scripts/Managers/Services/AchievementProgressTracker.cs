using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;
using JustClimb.Data;

namespace JustClimb.Services
{
    /// <summary>
    /// 업적 진행률 추적 및 조건 체크 전담 서비스
    /// </summary>
    public class AchievementProgressTracker
    {
        [Inject] private IDataManager _dataManager;

        /// <summary>
        /// 업적 진행률 저장 및 서버 동기화
        /// </summary>
        public void UpdateProgress(Action<AchievementProgressDto> updateAction)
        {
            if (_dataManager?.Current?.achievementProgress == null)
            {
                Debug.LogError("[AchievementProgressTracker] SaveData나 achievementProgress가 null입니다.");
                return;
            }

            try
            {
                var progress = _dataManager.Current.achievementProgress;
                updateAction(progress);
                
                // 서버 동기화
                _dataManager.GenerateDelta("achievementProgress", progress);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AchievementProgressTracker] 진행률 업데이트 중 예외 발생: {e.Message}");
            }
        }

        /// <summary>
        /// 캐시를 통한 업적 상태 확인
        /// </summary>
        public bool IsAchievementUnlocked(string achievementID)
        {
            if (_dataManager?.Current?.achievementUnlocked == null || string.IsNullOrEmpty(achievementID))
            {
                return false;
            }

            return _dataManager.Current.achievementUnlocked.ContainsKey(achievementID) &&
                   _dataManager.Current.achievementUnlocked[achievementID];
        }

        /// <summary>
        /// 업적 달성 상태 캐시 업데이트
        /// </summary>
        public void UpdateAchievementCache(string achievementID, bool isUnlocked)
        {
            if (_dataManager?.Current?.achievementUnlocked == null || string.IsNullOrEmpty(achievementID))
            {
                Debug.LogError("[AchievementProgressTracker] 업적 캐시 업데이트 실패 - 데이터가 null입니다.");
                return;
            }

            try
            {
                _dataManager.Current.achievementUnlocked[achievementID] = isUnlocked;
                
                // 서버 동기화
                _dataManager.GenerateDelta($"achievementUnlocked_{achievementID}", isUnlocked);
                
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.Log($"[AchievementProgressTracker] 업적 캐시 업데이트: {achievementID} = {isUnlocked}");
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AchievementProgressTracker] 업적 캐시 업데이트 중 예외 발생: {e.Message}");
            }
        }

        /// <summary>
        /// 보상 수령 상태 확인
        /// </summary>
        public bool IsRewardClaimed(string achievementId)
        {
            if (_dataManager?.Current?.achievementRewards == null)
            {
                return false;
            }

            return _dataManager.Current.achievementRewards.ContainsKey(achievementId) &&
                   _dataManager.Current.achievementRewards[achievementId];
        }

        /// <summary>
        /// 보상 수령 처리
        /// </summary>
        public void ClaimReward(string achievementId)
        {
            if (_dataManager?.Current?.achievementRewards == null)
            {
                Debug.LogError("[AchievementProgressTracker] achievementRewards가 null입니다.");
                return;
            }

            _dataManager.Current.achievementRewards[achievementId] = true;
            _dataManager.GenerateDelta($"achievementReward_{achievementId}", true);
        }

        /// <summary>
        /// 스테이지 시작 시 진행률 초기화
        /// </summary>
        public void OnStageStart()
        {
            UpdateProgress(progress =>
            {
                progress.deathsInCurrentStage = 0;
                progress.usedItemInCurrentStage = false;
            });

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[AchievementProgressTracker] 스테이지 시작 - 현재 스테이지 데이터 초기화");
#endif
        }

        /// <summary>
        /// 스테이지 클리어 진행률 업데이트
        /// </summary>
        public void UpdateStageProgress(int stageIndex, float clearTime, int deathCount, int gemsCollected, int totalGems)
        {
            UpdateProgress(progress =>
            {
                // 스테이지 클리어 카운트 증가
                progress.stagesCompleted++;

                // Perfect 클리어 체크 (사망 없음 + 모든 보석 수집)
                bool isPerfectClear = deathCount == 0 && gemsCollected == totalGems;
                if (isPerfectClear)
                {
                    progress.perfectClears++;
                    
                    // Chapter 1 Perfect 스테이지 추적
                    if (stageIndex >= 1 && stageIndex <= 4)
                    {
                        if (!progress.chapter1PerfectStages.Contains(stageIndex))
                        {
                            progress.chapter1PerfectStages.Add(stageIndex);
                        }
                    }
                }

                // Speed Clear 체크 (30초 이하)
                if (clearTime <= 30f)
                {
                    progress.speedClears++;
                }

                // 사망 횟수 누적
                progress.totalDeaths += deathCount;

                // 수집한 보석 누적
                progress.totalGemsCollected += gemsCollected;
            });
        }

        /// <summary>
        /// 플레이어 사망 시 진행률 업데이트
        /// </summary>
        public void OnPlayerDeath()
        {
            UpdateProgress(progress =>
            {
                progress.deathsInCurrentStage++;
                progress.totalDeaths++;
            });
        }

        /// <summary>
        /// 아이템 사용 시 진행률 업데이트
        /// </summary>
        public void OnItemUsed(string itemType)
        {
            UpdateProgress(progress =>
            {
                progress.usedItemInCurrentStage = true;
                
                // 사용한 아이템 타입 추가
                if (!progress.itemTypesUsed.Contains(itemType))
                {
                    progress.itemTypesUsed.Add(itemType);
                }
            });
        }

        /// <summary>
        /// 아이템 구매 시 진행률 업데이트
        /// </summary>
        public void OnItemPurchased(string itemName)
        {
            UpdateProgress(progress =>
            {
                progress.itemsPurchased++;
            });
        }

        /// <summary>
        /// 캐릭터 해제 시 진행률 업데이트
        /// </summary>
        public void OnCharacterUnlocked(int characterId)
        {
            UpdateProgress(progress =>
            {
                if (!progress.unlockedCharacters.Contains(characterId))
                {
                    progress.unlockedCharacters.Add(characterId);
                }
            });
        }

        /// <summary>
        /// 현재 진행률 가져오기 (읽기 전용)
        /// </summary>
        public AchievementProgressDto GetCurrentProgress()
        {
            return _dataManager?.Current?.achievementProgress;
        }
    }
} 