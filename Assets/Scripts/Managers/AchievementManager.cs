using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Zenject;
using JustClimb.Data;

/// <summary>
/// Steam 업적을 관리하는 매니저
/// 게임 이벤트를 받아서 업적 조건을 체크하고 Steam에 업적을 해제합니다.
/// </summary>
public class AchievementManager : MonoBehaviour, IAchievementManager
{
    [Inject] private GameManager _gameManager;
    [Inject] private IDataManager _dataManager;
    
    [Header("Steam 테스트 설정 (UI에서 동적으로 설정됨)")]
    [Tooltip("에디터에서도 실제 Steam API를 사용할지 여부 (런타임에 UI_Achievement에서 설정)")]
    [SerializeField] private bool _useRealSteamInEditor = false;

    // 업적 진행률은 SaveData에서 관리 (서버는 별도 테이블로 동기화)
    // Progress 프로퍼티 제거: 직접 _dataManager.Current.achievementProgress 참조 사용
    private bool _steamInitialized = false;
    private bool _steamStatsReady = false; // Steam 통계 준비 상태 캐시

    /// <summary>
    /// UI에서 Steam 테스트 설정을 적용하는 메서드
    /// </summary>
    public void SetUseRealSteamInEditor(bool useRealSteam)
    {
        _useRealSteamInEditor = useRealSteam;
        Debug.Log($"[AchievementManager] Steam 테스트 설정 변경: useRealSteamInEditor = {_useRealSteamInEditor}");
        
        // 설정 변경 시 Steam 초기화 상태 재검토
        if (_useRealSteamInEditor)
        {
            Debug.Log("[AchievementManager] 에디터에서 실제 Steam API 사용 모드로 변경");
        }
        else
        {
            Debug.Log("[AchievementManager] 에디터에서 개발 모드(로그만)로 변경");
        }
    }

    private void Start()
    {
        try
        {
            InitializeSteam();
            Debug.Log("[AchievementManager] Start 메서드 완료");
            // 데이터는 DataManager에서 자동으로 로드됨
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] Start에서 예외 발생: {e.Message}\n{e.StackTrace}");
        }
    }

    private void InitializeSteam()
    {
        try
        {
            // 캐시 상태 초기화
            _steamStatsReady = false;

            #if UNITY_EDITOR
            // 에디터에서는 UI 설정에 따라 동작 결정
            if (_useRealSteamInEditor)
            {
                Debug.Log("[AchievementManager] 에디터에서 실제 Steam API 사용 모드");
                // 실제 Steam API 초기화 시도
                if (!SteamAPI.IsSteamRunning())
                {
                    Debug.LogWarning("[AchievementManager] Steam이 실행되지 않았습니다. 개발 모드로 전환합니다.");
                    _steamInitialized = false;
                    return;
                }

                if (SteamManager.Initialized)
                {
                    _steamInitialized = true;
                    Debug.Log("[AchievementManager] Steam initialized successfully (Editor + Real Steam)");
                }
                else
                {
                    _steamInitialized = false;
                    Debug.Log("[AchievementManager] Steam not initialized. 개발 모드로 전환합니다.");
                }
            }
            else
            {
                Debug.Log("[AchievementManager] 개발 환경: Steam API 체크 건너뜀, 업적 시스템은 로그 모드로 동작");
                _steamInitialized = true; // 개발 환경에서는 항상 초기화된 것으로 처리
            }
            return;
            #endif

            // Steam API가 사용 가능한지 확인
            if (!SteamAPI.IsSteamRunning())
            {
                Debug.Log("[AchievementManager] Steam이 실행되지 않았습니다. 업적 시스템이 비활성화됩니다.");
                _steamInitialized = false;
                return;
            }

            if (SteamManager.Initialized)
            {
                _steamInitialized = true;
                Debug.Log("[AchievementManager] Steam initialized successfully");
            }
            else
            {
                _steamInitialized = false;
                Debug.Log("[AchievementManager] Steam not initialized. 업적 시스템이 비활성화됩니다.");
            }
        }
        catch (System.Exception e)
        {
            _steamInitialized = false;
            _steamStatsReady = false;
            Debug.LogError($"[AchievementManager] Steam 초기화 중 예외 발생: {e.Message}");
        }
    }

    /// <summary>
    /// Steam 통계를 안전하게 요청하는 메서드 (캐싱 지원)
    /// </summary>
    private bool EnsureSteamStatsReady()
    {
        // 이미 준비된 경우 캐시된 결과 반환
        if (_steamStatsReady)
        {
            return true;
        }

        if (!_steamInitialized || !SteamManager.Initialized)
        {
            return false;
        }

        // Steam API가 실행 중인지 확인
        if (!SteamAPI.IsSteamRunning())
        {
            return false;
        }

        try
        {
            #if UNITY_EDITOR
            if (_useRealSteamInEditor)
            {
                // 에디터에서 실제 Steam API 사용
                bool result = SteamUserStats.RequestCurrentStats();
                if (result)
                {
                    _steamStatsReady = true;
                    Debug.Log("[AchievementManager] Steam stats 준비 완료 (Editor + Real Steam)");
                }
                else
                {
                    Debug.Log("[AchievementManager] Steam stats 요청 실패 (Editor + Real Steam)");
                }
                return result;
            }
            else
            {
                // 개발 환경에서는 Steam 통계 요청을 건너뛰고 바로 성공으로 처리
                _steamStatsReady = true;
                Debug.Log("[AchievementManager] 개발 환경: Steam stats 요청 건너뜀");
                return true;
            }
            #else
            // 프로덕션에서만 실제 Steam 통계 요청
            bool result = SteamUserStats.RequestCurrentStats();
            if (result)
            {
                _steamStatsReady = true;
                Debug.Log("[AchievementManager] Steam stats 준비 완료");
            }
            else
            {
                Debug.Log("[AchievementManager] Steam stats 요청 실패");
            }
            return result;
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[AchievementManager] Steam stats 요청 중 예외: {e.Message}");
            return false;
        }
    }

    #region Achievement Unlock Methods

    /// <summary>
    /// Steam 업적 해제
    /// </summary>
    private void UnlockAchievement(string achievementID)
    {
        // 기본 유효성 검사
        if (!_steamInitialized || !SteamManager.Initialized || string.IsNullOrEmpty(achievementID))
        {
            Debug.Log($"[AchievementManager] Steam 조건 불만족, 업적 건너뜀: {achievementID}");
            return;
        }

        // Steam 통계 준비 확인
        if (!EnsureSteamStatsReady())
        {
            Debug.Log($"[AchievementManager] Steam stats not ready, skipping achievement: {achievementID}");
            return;
        }

        try
        {
            #if UNITY_EDITOR
            if (_useRealSteamInEditor)
            {
                // 에디터에서 실제 Steam API 사용
                bool success = SteamUserStats.SetAchievement(achievementID);
                if (success)
                {
                    bool storeResult = SteamUserStats.StoreStats();
                    if (storeResult)
                    {
                        Debug.Log($"[AchievementManager] [실제 Steam API] Achievement Unlocked: {achievementID}");
                        UpdateAchievementCache(achievementID, true);
                        ShowAchievementNotification(achievementID);
                    }
                    else
                    {
                        Debug.LogError($"[AchievementManager] [실제 Steam API] Failed to store stats for achievement: {achievementID}");
                    }
                }
                else
                {
                    Debug.LogError($"[AchievementManager] [실제 Steam API] Failed to unlock achievement: {achievementID}");
                }
            }
            else
            {
                // 개발 환경에서는 로그만 출력하고 캐시 업데이트
                Debug.Log($"[AchievementManager] [개발모드] Achievement Unlocked: {achievementID}");
                UpdateAchievementCache(achievementID, true);
                ShowAchievementNotification(achievementID);
            }
            #else
            // 프로덕션에서만 실제 Steam API 호출
            bool success = SteamUserStats.SetAchievement(achievementID);
            if (success)
            {
                bool storeResult = SteamUserStats.StoreStats();
                if (storeResult)
                {
                    Debug.Log($"[AchievementManager] Achievement Unlocked: {achievementID}");
                    UpdateAchievementCache(achievementID, true);
                    ShowAchievementNotification(achievementID);
                }
                else
                {
                    Debug.LogError($"[AchievementManager] Failed to store stats for achievement: {achievementID}");
                }
            }
            else
            {
                Debug.LogError($"[AchievementManager] Failed to unlock achievement: {achievementID}");
            }
            #endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] Exception while unlocking achievement {achievementID}: {e.Message}");
        }
    }

    /// <summary>
    /// 업적 달성 여부 확인 (클라이언트 캐시 우선, Steam API 보조)
    /// </summary>
    public bool IsAchievementUnlocked(string achievementID)
    {
        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementUnlocked == null || string.IsNullOrEmpty(achievementID))
        {
            return false;
        }

        // 1. 클라이언트 캐시에서 먼저 확인
        if (_dataManager.Current.achievementUnlocked.ContainsKey(achievementID))
        {
            return _dataManager.Current.achievementUnlocked[achievementID];
        }

        // 2. Steam API로 확인 (캐시에 없는 경우)
        try
        {
            #if UNITY_EDITOR
            if (_useRealSteamInEditor)
            {
                // 에디터에서 실제 Steam API 사용
                if (_steamInitialized && SteamManager.Initialized && EnsureSteamStatsReady())
                {
                    bool achieved;
                    bool success = SteamUserStats.GetAchievement(achievementID, out achieved);
                    if (success)
                    {
                        // Steam에서 가져온 결과를 캐시에 저장
                        _dataManager.Current.achievementUnlocked[achievementID] = achieved;
                        _dataManager.SaveLocal();
                        return achieved;
                    }
                }
                return false;
            }
            else
            {
                // 개발 환경에서는 캐시된 값만 사용
                return false;
            }
            #else
            if (_steamInitialized && SteamManager.Initialized && EnsureSteamStatsReady())
            {
                bool achieved;
                bool success = SteamUserStats.GetAchievement(achievementID, out achieved);
                if (success)
                {
                    // Steam에서 가져온 결과를 캐시에 저장
                    _dataManager.Current.achievementUnlocked[achievementID] = achieved;
                    _dataManager.SaveLocal();
                    return achieved;
                }
            }
            return false;
            #endif
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] Exception while checking achievement {achievementID}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 업적 해제 알림 표시
    /// </summary>
    private void ShowAchievementNotification(string achievementID)
    {
        // TODO: UI 매니저를 통해 업적 해제 알림 표시
        Debug.Log($"🏆 업적 달성: {achievementID}");
    }

    /// <summary>
    /// 클라이언트 업적 캐시 업데이트
    /// </summary>
    private void UpdateAchievementCache(string achievementID, bool isUnlocked)
    {
        if (_dataManager?.Current?.achievementUnlocked == null || string.IsNullOrEmpty(achievementID))
        {
            return;
        }

        try
        {
            _dataManager.Current.achievementUnlocked[achievementID] = isUnlocked;
            _dataManager.SaveLocal();
            
            // 서버에 동기화
            _dataManager.GenerateDelta($"achievementUnlocked_{achievementID}", isUnlocked);
            
            Debug.Log($"[AchievementManager] 업적 캐시 업데이트: {achievementID} = {isUnlocked}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] UpdateAchievementCache 실패: {e.Message}");
        }
    }

    #endregion

    #region Game Event Handlers

    /// <summary>
    /// 스테이지 시작 시 호출
    /// </summary>
    public void OnStageStart()
    {
        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementProgress == null)
        {
            Debug.LogWarning("[AchievementManager] SaveData가 없어서 스테이지 시작 처리를 건너뜁니다.");
            return;
        }

        try
        {
            var progress = _dataManager.Current.achievementProgress;
            progress.deathsInCurrentStage = 0;
            progress.usedItemInCurrentStage = false;
            
            Debug.Log("[AchievementManager] 스테이지 시작 - 현재 스테이지 데이터 초기화");
            
            // 기존 DataManager 시스템을 통한 서버 동기화
            _dataManager.GenerateDelta("achievementProgress", progress);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] OnStageStart에서 예외 발생: {e.Message}");
        }
    }

    /// <summary>
    /// 스테이지 클리어 시 호출
    /// </summary>
    public void OnStageCleared(int stageIndex, float clearTime, int deathCount, int gemsCollected, int totalGems)
    {
        Debug.Log($"[AchievementManager] OnStageCleared 호출 - Stage: {stageIndex}, Time: {clearTime}, Deaths: {deathCount}");

        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementProgress == null)
        {
            Debug.LogError("[AchievementManager] SaveData나 achievementProgress가 null입니다. 업적 처리를 건너뜁니다.");
            return;
        }

        try
        {
            // Progress 프로퍼티 대신 직접 참조 사용 (안전함)
            var progress = _dataManager.Current.achievementProgress;
            progress.stagesCompleted++;
            
            Debug.Log($"[AchievementManager] 스테이지 클리어 카운트 증가: {progress.stagesCompleted}");
            
            // 기본 클리어 업적 체크
            CheckBasicClearAchievements(stageIndex);
            
            // 완벽한 클리어 체크 (무사망 + 모든 젬 수집)
            bool isPerfectClear = deathCount == 0 && gemsCollected >= totalGems;
            if (isPerfectClear)
            {
                progress.perfectClears++;
                Debug.Log($"[AchievementManager] 완벽 클리어 달성: {progress.perfectClears}");
                CheckPerfectClearAchievements(stageIndex);
            }
            
            // 스피드 클리어 체크 (30초 이내)
            if (clearTime <= 30f)
            {
                progress.speedClears++;
                Debug.Log($"[AchievementManager] 스피드 클리어 달성: {progress.speedClears}");
                CheckSpeedClearAchievements();
            }
            
            // 완벽주의자 업적 (무사망 클리어)
            if (deathCount == 0 && !IsAchievementUnlocked(AchievementIDs.PERFECTIONIST))
            {
                Debug.Log("[AchievementManager] 완벽주의자 업적 해제 시도");
                UnlockAchievement(AchievementIDs.PERFECTIONIST);
            }
            
            // 좀비 업적 (100번 이상 사망 후 클리어)
            if (progress.deathsInCurrentStage >= 100 && !IsAchievementUnlocked(AchievementIDs.ZOMBIE))
            {
                Debug.Log("[AchievementManager] 좀비 업적 해제 시도");
                UnlockAchievement(AchievementIDs.ZOMBIE);
            }
            
            // 맨손 등반가 업적 (아이템 사용 없이 클리어)
            if (!progress.usedItemInCurrentStage && !IsAchievementUnlocked(AchievementIDs.NATURAL_CLIMBER))
            {
                Debug.Log("[AchievementManager] 맨손 등반가 업적 해제 시도");
                UnlockAchievement(AchievementIDs.NATURAL_CLIMBER);
            }
            
            // 서버에 데이터 동기화
            _dataManager.GenerateDelta("achievementProgress", progress);
            
            Debug.Log($"[AchievementManager] 스테이지 {stageIndex} 클리어 처리 완료 - 총 클리어: {progress.stagesCompleted}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] OnStageCleared에서 예외 발생: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 플레이어 사망 시 호출
    /// </summary>
    public void OnPlayerDeath()
    {
        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementProgress == null)
        {
            Debug.LogWarning("[AchievementManager] SaveData가 없어서 사망 처리를 건너뜁니다.");
            return;
        }

        try
        {
            var progress = _dataManager.Current.achievementProgress;
            progress.deathsInCurrentStage++;
            
            Debug.Log($"[AchievementManager] 플레이어 사망 - 현재 스테이지 사망 횟수: {progress.deathsInCurrentStage}");
            
            // 데이터 변경 알림 (사망 횟수는 자주 변경되므로 로컬만 저장)
            _dataManager.SaveLocal();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] OnPlayerDeath에서 예외 발생: {e.Message}");
        }
    }

    /// <summary>
    /// 아이템 사용 시 호출
    /// </summary>
    public void OnItemUsed(string itemType)
    {
        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementProgress == null)
        {
            Debug.LogWarning("[AchievementManager] SaveData가 없어서 아이템 사용 처리를 건너뜁니다.");
            return;
        }

        try
        {
            var progress = _dataManager.Current.achievementProgress;
            progress.usedItemInCurrentStage = true;
            if (!progress.itemTypesUsed.Contains(itemType))
            {
                progress.itemTypesUsed.Add(itemType);
            }
            
            CheckItemUsageAchievements();
            
            // 서버에 데이터 동기화
            _dataManager.GenerateDelta("achievementProgress", progress);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] OnItemUsed에서 예외 발생: {e.Message}");
        }
    }

    /// <summary>
    /// 아이템 구매 시 호출
    /// </summary>
    public void OnItemPurchased(string itemType)
    {
        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementProgress == null)
        {
            Debug.LogWarning("[AchievementManager] SaveData가 없어서 아이템 구매 처리를 건너뜁니다.");
            return;
        }

        try
        {
            var progress = _dataManager.Current.achievementProgress;
            progress.itemsPurchased++;
            
            CheckItemPurchaseAchievements();
            
            // 서버에 데이터 동기화
            _dataManager.GenerateDelta("achievementProgress", progress);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] OnItemPurchased에서 예외 발생: {e.Message}");
        }
    }

    /// <summary>
    /// 캐릭터 해제 시 호출
    /// </summary>
    public void OnCharacterUnlocked(string characterName)
    {
        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementProgress == null)
        {
            Debug.LogWarning("[AchievementManager] SaveData가 없어서 캐릭터 해제 처리를 건너뜁니다.");
            return;
        }

        try
        {
            var progress = _dataManager.Current.achievementProgress;
            if (!progress.unlockedCharacters.Contains(characterName))
            {
                progress.unlockedCharacters.Add(characterName);
            }
            
            CheckCharacterUnlockAchievements(characterName);
            
            // 서버에 데이터 동기화
            _dataManager.GenerateDelta("achievementProgress", progress);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] OnCharacterUnlocked에서 예외 발생: {e.Message}");
        }
    }

    #endregion

    #region Achievement Check Methods

    private void CheckBasicClearAchievements(int stageIndex)
    {
        // 첫 번째 스테이지 클리어
        if (stageIndex == 1 && !IsAchievementUnlocked(AchievementIDs.NOVICE_CLIMBER))
        {
            UnlockAchievement(AchievementIDs.NOVICE_CLIMBER);
        }
        
        // 5번째 스테이지 클리어
        if (stageIndex == 5 && !IsAchievementUnlocked(AchievementIDs.INTERMEDIATE_CLIMBER))
        {
            UnlockAchievement(AchievementIDs.INTERMEDIATE_CLIMBER);
        }
        
        // 10번째 스테이지 클리어
        if (stageIndex == 10 && !IsAchievementUnlocked(AchievementIDs.ADVANCED_CLIMBER))
        {
            UnlockAchievement(AchievementIDs.ADVANCED_CLIMBER);
        }
        
        // 챕터별 완주 체크
        CheckChapterCompletionAchievements(stageIndex);
    }

    private void CheckChapterCompletionAchievements(int stageIndex)
    {
        // 각 챕터의 마지막 스테이지 클리어 시 업적 해제
        // (실제 게임의 챕터 구조에 맞게 조정 필요)
        
        if (stageIndex == 10 && !IsAchievementUnlocked(AchievementIDs.CHAPTER_1_MASTER)) // Chapter 1 마지막
        {
            UnlockAchievement(AchievementIDs.CHAPTER_1_MASTER);
        }
        
        if (stageIndex == 20 && !IsAchievementUnlocked(AchievementIDs.CHAPTER_2_MASTER)) // Chapter 2 마지막
        {
            UnlockAchievement(AchievementIDs.CHAPTER_2_MASTER);
        }
        
        if (stageIndex == 30 && !IsAchievementUnlocked(AchievementIDs.CHAPTER_3_MASTER)) // Chapter 3 마지막
        {
            UnlockAchievement(AchievementIDs.CHAPTER_3_MASTER);
        }
        
        if (stageIndex == 40 && !IsAchievementUnlocked(AchievementIDs.CHAPTER_4_MASTER)) // Chapter 4 마지막
        {
            UnlockAchievement(AchievementIDs.CHAPTER_4_MASTER);
        }
        
        if (stageIndex == 50 && !IsAchievementUnlocked(AchievementIDs.CHAPTER_5_MASTER)) // Chapter 5 마지막
        {
            UnlockAchievement(AchievementIDs.CHAPTER_5_MASTER);
        }
        
        // 모든 챕터 완주 (산신)
        if (IsAchievementUnlocked(AchievementIDs.CHAPTER_1_MASTER) &&
            IsAchievementUnlocked(AchievementIDs.CHAPTER_2_MASTER) &&
            IsAchievementUnlocked(AchievementIDs.CHAPTER_3_MASTER) &&
            IsAchievementUnlocked(AchievementIDs.CHAPTER_4_MASTER) &&
            IsAchievementUnlocked(AchievementIDs.CHAPTER_5_MASTER) &&
            !IsAchievementUnlocked(AchievementIDs.MOUNTAIN_GOD))
        {
            UnlockAchievement(AchievementIDs.MOUNTAIN_GOD);
        }
    }

    private void CheckPerfectClearAchievements(int stageIndex)
    {
        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementProgress == null) return;
        
        var progress = _dataManager.Current.achievementProgress;
        
        // 완벽한 등반 (5개 스테이지 완벽 클리어)
        if (progress.perfectClears >= 5 && !IsAchievementUnlocked(AchievementIDs.FLAWLESS_CLIMB))
        {
            UnlockAchievement(AchievementIDs.FLAWLESS_CLIMB);
        }
        
        // Chapter 1 완벽 클리어 추적
        if (stageIndex <= 10) // Chapter 1 범위
        {
            progress.chapter1PerfectStages++;
            
            // 언터처블 (Chapter 1 모든 스테이지 완벽 클리어)
            if (progress.chapter1PerfectStages >= 10 && !IsAchievementUnlocked(AchievementIDs.UNTOUCHABLE))
            {
                UnlockAchievement(AchievementIDs.UNTOUCHABLE);
            }
        }
    }

    private void CheckSpeedClearAchievements()
    {
        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementProgress == null) return;
        
        var progress = _dataManager.Current.achievementProgress;
        
        // 야빠른 평지러님 (30초 이내 클리어)
        if (progress.speedClears >= 1 && !IsAchievementUnlocked(AchievementIDs.SPEED_CLIMBER))
        {
            UnlockAchievement(AchievementIDs.SPEED_CLIMBER);
        }
    }

    private void CheckCharacterUnlockAchievements(string characterName)
    {
        switch (characterName.ToLower())
        {
            case "braden":
                if (!IsAchievementUnlocked(AchievementIDs.UNLOCK_BRADEN))
                {
                    UnlockAchievement(AchievementIDs.UNLOCK_BRADEN);
                }
                break;
                
            case "lina":
                if (!IsAchievementUnlocked(AchievementIDs.UNLOCK_LINA))
                {
                    UnlockAchievement(AchievementIDs.UNLOCK_LINA);
                }
                break;
                
            case "elliott":
                if (!IsAchievementUnlocked(AchievementIDs.UNLOCK_ELLIOTT))
                {
                    UnlockAchievement(AchievementIDs.UNLOCK_ELLIOTT);
                }
                break;
        }
    }

    private void CheckItemPurchaseAchievements()
    {
        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementProgress == null) return;
        
        var progress = _dataManager.Current.achievementProgress;
        
        // 첫 구매
        if (progress.itemsPurchased == 1 && !IsAchievementUnlocked(AchievementIDs.FIRST_PURCHASE))
        {
            UnlockAchievement(AchievementIDs.FIRST_PURCHASE);
        }
        
        // VIP 고객 (10개 이상 구매)
        if (progress.itemsPurchased >= 10 && !IsAchievementUnlocked(AchievementIDs.SHOP_VIP))
        {
            UnlockAchievement(AchievementIDs.SHOP_VIP);
        }
        
        // 수집가 (모든 종류 구매) - 실제 아이템 종류 수에 맞게 조정 필요
        if (progress.itemsPurchased >= 20 && !IsAchievementUnlocked(AchievementIDs.COLLECTOR))
        {
            UnlockAchievement(AchievementIDs.COLLECTOR);
        }
    }

    private void CheckItemUsageAchievements()
    {
        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementProgress == null) return;
        
        var progress = _dataManager.Current.achievementProgress;
        
        // 도구 마스터 (모든 종류 아이템 사용) - 실제 아이템 종류 수에 맞게 조정 필요
        if (progress.itemTypesUsed.Count >= 10 && !IsAchievementUnlocked(AchievementIDs.TOOL_MASTER))
        {
            UnlockAchievement(AchievementIDs.TOOL_MASTER);
        }
    }

    #endregion

    #region Data Management

    /// <summary>
    /// 업적 보상 수령 상태 확인
    /// </summary>
    public bool IsRewardClaimed(string achievementId)
    {
        return _dataManager.Current.achievementRewards.ContainsKey(achievementId) && 
               _dataManager.Current.achievementRewards[achievementId];
    }

    /// <summary>
    /// 업적 보상 수령 처리
    /// </summary>
    public void ClaimReward(string achievementId)
    {
        _dataManager.Current.achievementRewards[achievementId] = true;
        
        // 서버에 동기화
        _dataManager.GenerateDelta("achievementRewards", _dataManager.Current.achievementRewards);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 특정 업적 강제 해제 (테스트용)
    /// </summary>
    [ContextMenu("Test Unlock Achievement")]
    public void TestUnlockAchievement()
    {
        UnlockAchievement(AchievementIDs.NOVICE_CLIMBER);
    }

    /// <summary>
    /// 첫 번째 스테이지 클리어 업적 강제 해제 (테스트용)
    /// </summary>
    [ContextMenu("Test First Stage Clear")]
    public void TestFirstStageClear()
    {
        // 첫 번째 스테이지 클리어 시뮬레이션
        OnStageCleared(1, 25f, 2, 3, 3);
        Debug.Log("테스트: 첫 번째 스테이지 클리어 시뮬레이션 완료");
    }

    /// <summary>
    /// 아이템 구매 업적 강제 해제 (테스트용)
    /// </summary>
    [ContextMenu("Test Item Purchase")]
    public void TestItemPurchase()
    {
        // 첫 구매 업적 시뮬레이션
        OnItemPurchased("TestItem");
        Debug.Log("테스트: 아이템 구매 시뮬레이션 완료");
    }

    /// <summary>
    /// 모든 업적 리셋 (테스트용)
    /// </summary>
    [ContextMenu("Reset All Achievements")]
    public void ResetAllAchievements()
    {
        if (_steamInitialized)
        {
            SteamUserStats.ResetAllStats(true); // true = 업적도 함께 리셋
            
            // 데이터 초기화
            _dataManager.Current.achievementProgress = new AchievementProgressDto();
            _dataManager.Current.achievementRewards.Clear();
            
            // 서버에 동기화
            _dataManager.GenerateDelta("achievementProgress", _dataManager.Current.achievementProgress);
            _dataManager.GenerateDelta("achievementRewards", _dataManager.Current.achievementRewards);
            
            Debug.Log("All achievements reset!");
        }
    }

    /// <summary>
    /// 현재 진행률 출력 (디버그용)
    /// </summary>
    [ContextMenu("Print Progress")]
    public void PrintProgress()
    {
        if (_dataManager?.Current?.achievementProgress == null)
        {
            Debug.LogWarning("[AchievementManager] SaveData가 없어서 진행률을 출력할 수 없습니다.");
            return;
        }

        var progress = _dataManager.Current.achievementProgress;
        
        Debug.Log($"=== Achievement Progress ===");
        Debug.Log($"Stages Completed: {progress.stagesCompleted}");
        Debug.Log($"Perfect Clears: {progress.perfectClears}");
        Debug.Log($"Speed Clears: {progress.speedClears}");
        Debug.Log($"Items Purchased: {progress.itemsPurchased}");
        Debug.Log($"Characters Unlocked: {progress.unlockedCharacters.Count}");
        Debug.Log($"Item Types Used: {progress.itemTypesUsed.Count}");
        Debug.Log($"Chapter 1 Perfect Stages: {progress.chapter1PerfectStages}");
    }

    #endregion
} 