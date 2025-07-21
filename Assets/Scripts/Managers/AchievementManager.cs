using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Zenject;
using JustClimb.Data;
using JustClimb.Services;

/// <summary>
/// 업적 시스템의 메인 Facade 매니저
/// 분리된 서비스들을 조합하여 통합된 API를 제공합니다.
/// </summary>
public class AchievementManager : MonoBehaviour, IAchievementManager
{
    [Inject] private GameManager _gameManager;
    [Inject] private IDataManager _dataManager;
    [Inject] private DiContainer _container;

    // 분리된 서비스들
    private SteamAchievementService _steamService;
    private AchievementProgressTracker _progressTracker;
    private AchievementEventHandler _eventHandler;

    [Header("Steam 테스트 설정 (UI에서 동적으로 설정됨)")]
    [Tooltip("에디터에서도 실제 Steam API를 사용할지 여부 (런타임에 UI_Achievement에서 설정)")]
    [SerializeField] private bool _useRealSteamInEditor = false;

    #region Unity Lifecycle

    private void Start()
    {
        try
        {
            InitializeServices();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[AchievementManager] Start 메서드 완료");
#endif
            // 데이터는 DataManager에서 자동으로 로드됨
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] Start에서 예외 발생: {e.Message}\n{e.StackTrace}");
        }
    }

    private void OnDestroy()
    {
        if (_eventHandler != null)
        {
            _eventHandler.OnAchievementUnlocked -= HandleAchievementUnlocked;
        }
    }

    #endregion

    #region Service Initialization

    /// <summary>
    /// 분리된 서비스들 초기화
    /// </summary>
    private void InitializeServices()
    {
        // Steam 서비스 초기화
        _steamService = new SteamAchievementService();
        _steamService.InitializeSteam();
        _steamService.SetUseRealSteamInEditor(_useRealSteamInEditor);

        // 진행률 추적 서비스 초기화 (DI 필요)
        _progressTracker = new AchievementProgressTracker();
        _container.Inject(_progressTracker);

        // 이벤트 핸들러 초기화 (DI로 의존성 주입)
        _eventHandler = new AchievementEventHandler(_progressTracker);
        _container.Inject(_eventHandler);
        
        // 이벤트 구독
        _eventHandler.OnAchievementUnlocked += HandleAchievementUnlocked;
    }

    #endregion

    #region Public API (IAchievementManager Implementation)

    /// <summary>
    /// UI에서 Steam 테스트 설정을 적용하는 메서드
    /// </summary>
    public void SetUseRealSteamInEditor(bool useRealSteam)
    {
        _useRealSteamInEditor = useRealSteam;
        _steamService?.SetUseRealSteamInEditor(useRealSteam);
    }

    /// <summary>
    /// 업적 달성 여부 확인
    /// </summary>
    public bool IsAchievementUnlocked(string achievementId)
    {
        return _progressTracker?.IsAchievementUnlocked(achievementId) ?? false;
    }

    /// <summary>
    /// 보상 수령 상태 확인
    /// </summary>
    public bool IsRewardClaimed(string achievementId)
    {
        return _progressTracker?.IsRewardClaimed(achievementId) ?? false;
    }

    /// <summary>
    /// 보상 수령 처리
    /// </summary>
    public void ClaimReward(string achievementId)
    {
        _progressTracker?.ClaimReward(achievementId);
    }

    #endregion

    #region Game Event Handlers

    /// <summary>
    /// 스테이지 시작 시 호출
    /// </summary>
    public void OnStageStart()
    {
        try
        {
            _progressTracker?.OnStageStart();
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
        // 데이터 유효성 체크
        if (_dataManager?.Current?.achievementProgress == null)
        {
            Debug.LogError("[AchievementManager] SaveData나 achievementProgress가 null입니다. 업적 처리를 건너뜁니다.");
            return;
        }

        try
        {
            _eventHandler?.HandleStageCleared(stageIndex, clearTime, deathCount, gemsCollected, totalGems);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] OnStageCleared에서 예외 발생: {e.Message}");
        }
    }

    /// <summary>
    /// 플레이어 사망 시 호출
    /// </summary>
    public void OnPlayerDeath()
    {
        try
        {
            _eventHandler?.HandlePlayerDeath();
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
        try
        {
            _eventHandler?.HandleItemUsed(itemType);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] OnItemUsed에서 예외 발생: {e.Message}");
        }
    }

    /// <summary>
    /// 아이템 구매 시 호출
    /// </summary>
    public void OnItemPurchased(string itemName)
    {
        try
        {
            _eventHandler?.HandleItemPurchased(itemName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] OnItemPurchased에서 예외 발생: {e.Message}");
        }
    }

    /// <summary>
    /// 캐릭터 해제 시 호출
    /// </summary>
    public void OnCharacterUnlocked(int characterId)
    {
        try
        {
            _eventHandler?.HandleCharacterUnlocked(characterId);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] OnCharacterUnlocked에서 예외 발생: {e.Message}");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 업적 달성 처리
    /// </summary>
    private void HandleAchievementUnlocked(string achievementID)
    {
        try
        {
            // Steam에서 업적 해제 처리
            bool steamSuccess = _steamService?.UnlockSteamAchievement(achievementID) ?? false;
            
            // 캐시 업데이트 (Steam 성공 여부와 관계없이)
            _progressTracker?.UpdateAchievementCache(achievementID, true);
            
            // UI 알림 표시
            ShowAchievementNotification(achievementID);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementManager] 업적 달성 처리 중 예외 발생 {achievementID}: {e.Message}");
        }
    }

    /// <summary>
    /// 업적 달성 알림 표시
    /// </summary>
    private void ShowAchievementNotification(string achievementID)
    {
        // TODO: UI 매니저를 통해 업적 해제 알림 표시
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Debug.Log($"🏆 업적 달성: {achievementID}");
#endif
    }

    #endregion
} 