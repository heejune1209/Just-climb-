using UnityEngine;
using Zenject;
using JustClimb.Data;

/// <summary>
/// 게임의 다른 시스템들과 AchievementManager를 연동하는 유틸리티 클래스
/// 다른 스크립트에서 이 클래스를 통해 업적 이벤트를 발생시킬 수 있습니다.
/// </summary>
public static class AchievementIntegration
{
    private static IAchievementManager _achievementManager;
    
    /// <summary>
    /// AchievementManager 참조 설정 (게임 시작 시 호출)
    /// </summary>
    public static void Initialize(IAchievementManager achievementManager)
    {
        _achievementManager = achievementManager;
        Debug.Log("Achievement Integration Initialized");
    }

    #region Stage Events

    /// <summary>
    /// 스테이지 시작 시 호출
    /// GameManager나 Stage 관련 스크립트에서 호출
    /// </summary>
    public static void OnStageStart()
    {
        if (_achievementManager == null)
        {
            Debug.LogWarning("[AchievementIntegration] AchievementManager가 초기화되지 않았습니다.");
            return;
        }
        
        try
        {
            _achievementManager.OnStageStart();
            Debug.Log("[AchievementIntegration] Stage Started");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementIntegration] OnStageStart 예외: {e.Message}");
        }
    }

    /// <summary>
    /// 스테이지 클리어 시 호출
    /// GameManager나 Stage 관련 스크립트에서 호출
    /// </summary>
    public static void OnStageCleared(int stageIndex, float clearTime, int deathCount, int gemsCollected, int totalGems)
    {
        if (_achievementManager == null)
        {
            Debug.LogWarning("[AchievementIntegration] AchievementManager가 초기화되지 않았습니다.");
            return;
        }

        try
        {
            _achievementManager.OnStageCleared(stageIndex, clearTime, deathCount, gemsCollected, totalGems);
            Debug.Log($"[AchievementIntegration] Stage {stageIndex} cleared - Time: {clearTime}s, Deaths: {deathCount}, Gems: {gemsCollected}/{totalGems}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementIntegration] OnStageCleared 예외: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 플레이어 사망 시 호출
    /// Player 스크립트에서 호출
    /// </summary>
    public static void OnPlayerDeath()
    {
        if (_achievementManager == null)
        {
            Debug.LogWarning("[AchievementIntegration] AchievementManager가 초기화되지 않았습니다.");
            return;
        }
        
        try
        {
            _achievementManager.OnPlayerDeath();
            Debug.Log("[AchievementIntegration] Player died");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AchievementIntegration] OnPlayerDeath 예외: {e.Message}");
        }
    }

    #endregion

    #region Character Events

    /// <summary>
    /// 캐릭터 해제 시 호출
    /// CharacterManager나 Shop에서 호출
    /// </summary>
    public static void OnCharacterUnlocked(string characterName)
    {
        if (_achievementManager == null) return;
        
        _achievementManager.OnCharacterUnlocked(characterName);
        Debug.Log($"Achievement: Character unlocked - {characterName}");
    }

    #endregion

    #region Item Events

    /// <summary>
    /// 아이템 구매 시 호출
    /// Shop이나 Inventory에서 호출
    /// </summary>
    public static void OnItemPurchased(string itemType)
    {
        if (_achievementManager == null) return;
        
        _achievementManager.OnItemPurchased(itemType);
        Debug.Log($"Achievement: Item purchased - {itemType}");
    }

    /// <summary>
    /// 아이템 사용 시 호출
    /// Inventory나 관련 스크립트에서 호출
    /// </summary>
    public static void OnItemUsed(string itemType)
    {
        if (_achievementManager == null) return;
        
        _achievementManager.OnItemUsed(itemType);
        Debug.Log($"Achievement: Item used - {itemType}");
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 현재 진행률 확인 (디버그용)
    /// </summary>
    public static void PrintCurrentProgress()
    {
        if (_achievementManager == null)
        {
            Debug.LogWarning("AchievementManager not initialized");
            return;
        }
        
        _achievementManager.PrintProgress();
    }

    /// <summary>
    /// 테스트용 업적 해제
    /// </summary>
    public static void TestUnlockAchievement()
    {
        if (_achievementManager == null) return;
        
        _achievementManager.TestUnlockAchievement();
    }

    #endregion
}

/// <summary>
/// AchievementManager를 자동으로 찾아서 초기화하는 컴포넌트
/// GameManager나 메인 씬에 추가
/// </summary>
public class AchievementInitializer : MonoBehaviour
{
    [Inject] private IAchievementManager _achievementManager;
    
    private void Start()
    {
        // AchievementIntegration 초기화
        AchievementIntegration.Initialize(_achievementManager);
    }
} 