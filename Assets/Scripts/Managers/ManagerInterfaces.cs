using System;
using System.Collections.Generic;
using UnityEngine;
using JustClimb.Data;
using JustClimb.Items;
using JustClimb.Manager;
using Zenject;

/// <summary>
/// Manager interfaces for dependency injection.
/// </summary>
public interface IDataManager : IDisposable
{
    SaveData Current { get; }
    event Action<SaveData> OnLoaded;
    event Action<SaveData> OnSaved;

    // <summary>
    /// 필드 델타가 생성될 때마다 발생합니다.
    /// OfflineCacheManager가 이걸 구독해서 오프라인 캐싱에 사용.
    /// </summary>
    event Action<DeltaEvent> OnDeltaGenerated;

    void Load();
    void Save();

    // 로컬 저장만, 델타 발생 없이
    void SaveLocal();

    // 풀-덤프 델타 별도 호출
    void GenerateFullDelta();

    // 키·값 기반 델타 생성 메서드
    void GenerateDelta(string key, object value);
}

public interface IDataSyncManager
{
    void EnqueueDelta(DeltaEvent delta);

    /// <summary>동기화 코루틴을 일시 중지.</summary>
    void PauseSync();

    /// <summary>동기화 코루틴을 재개.</summary>
    void ResumeSync();
}

public interface ICurrencyManager
{
    /// <summary>
    /// 현재 골드 양
    /// </summary>
    int Gold { get; }
    
    /// <summary>
    /// 현재 보석 양
    /// </summary>
    int Gems { get; }
    
    event Action<int> OnGoldChanged;
    event Action<int> OnGemsChanged;
    void Init();
    int GetGold();
    int GetGems();
    void AddGold(int amount);
    bool SpendGold(int amount);
    void AddGems(int amount);
    bool SpendGems(int amount);
}

public interface IItemManager
{
    event Action<ItemType, int> OnItemCountChanged;
    void Init();
    int GetItemCount(ItemType itemId);
    void AddItem(ItemType itemId, int amount = 1);
    bool UseItem(ItemType itemId, GameObject user);
    void RemoveItem(ItemType itemId, int amount = 1);
    float GetBuffRemaining(ItemType itemId);
    float GetBuffDuration(ItemType itemId);
    float GetCooldownRemaining(ItemType itemId);
    float GetCooldownDuration(ItemType itemId);
    IEnumerable<ItemType> GetAllItemIds();
}

public interface IStageManager : IDisposable
{
    event Action<int, int> OnBestRewardUpdated;
    event Action<int, float> OnBestTimeUpdated;
    event Action<int, int> OnBestDeathUpdated;

    event Action<int> OnStageUnlocked;

    void Init();
    bool IsUnlocked(int stageNum);

    int GetBestReward(int stageNum);
    float GetBestTime(int stageNum);
    int GetBestDeath(int stageNum);
    void SetCleared(int stageNum, int gemCount, float clearTime, int deathCount);
}

public interface IRankingManager : IDisposable
{
    event Action<int, RankingSortType> OnRankingUpdated;
    void Init();
    void InvalidateCache(int stageNum);
    void InvalidateAllCache();
    IReadOnlyList<RankingEntry> GetRanking(int stageNum, RankingSortType sortType);
    (IReadOnlyList<RankingEntry> topEntries, RankingEntry myEntry) GetRankingWithMyEntry(int stageNum, RankingSortType sortType, int maxTopEntries = 20);
}

public interface IGameManager
{
    bool IsTimerPaused { get; set; }
    event Action<TimeSpan> OnTimerUpdated;
    event Action<int> OnDeathCountChanged;
    int PlayerDeathCount { get; set; }
    Vector3 FlagPosition { get; }
    bool HasFlagPosition { get; }
    TimeSpan ElapsedTime();
    void Init();
    void OnPlayerDead();

    void OnStageCleared();
    void SaveFlagPosition(Vector3 pos);
}

public interface IResourceManager
{
    T Load<T>(string path) where T : UnityEngine.Object;
    T[] LoadAll<T>(string path) where T : UnityEngine.Object;
    GameObject Instantiate(string path, Transform parent = null, int count = 5);

    GameObject Instantiate(string path, Vector3 worldPos, Quaternion worldRot, Transform parent = null, int count = 5);
    void Destroy(GameObject go);
}

/// <summary>
/// UI 관리 인터페이스
/// </summary>
public interface IUIManager
{
    /// <summary>
    /// 캔버스 설정
    /// </summary>
    void SetCanvas(GameObject go, bool sort = true);
    
    /// <summary>
    /// 서브 아이템 생성
    /// </summary>
    T MakeSubItem<T>(Transform parent = null, string name = null) where T : UI_Base;
    
    /// <summary>
    /// 씬 UI 표시
    /// </summary>
    T ShowSceneUI<T>(string name = null) where T : UI_Scene;
    
    /// <summary>
    /// 팝업 UI 표시
    /// </summary>
    T ShowPopupUI<T>(string name = null) where T : UI_Popup;
    
    /// <summary>
    /// 최상위 팝업 가져오기
    /// </summary>
    UI_Popup GetTopPopup();
    
    /// <summary>
    /// 특정 팝업이 열려있는지 확인
    /// </summary>
    bool IsPopupOpen<T>() where T : UI_Popup;
    
    /// <summary>
    /// UI 루트 오브젝트
    /// </summary>
    GameObject Root { get; }
    
    /// <summary>
    /// 팝업 닫기 (안전)
    /// </summary>
    void ClosePopupUI(UI_Popup popup);
    
    /// <summary>
    /// 최상위 팝업 닫기
    /// </summary>
    void ClosePopupUI();
    
    /// <summary>
    /// 모든 팝업 닫기
    /// </summary>
    void CloseAllPopupUI();
    
    /// <summary>
    /// 팝업 정리
    /// </summary>
    void ClearPopupUI();
    
    /// <summary>
    /// 씬 UI 정리
    /// </summary>
    void ClearSceneUI();
    
    /// <summary>
    /// 모든 UI 정리
    /// </summary>
    void ClearAllUI();
}

public interface ISceneManagerEx
{
    BaseScene CurrentScene { get; }
    string GetSceneName(Define.Scene type);
    void LoadScene(Define.Scene type);
}

/// <summary>
/// 사운드 관리 인터페이스
/// </summary>
public interface ISoundManager
{
    void Init();
    void PlayBGM(int index);
    void PlayBGM(string path);
    void StopBGM();
    void PlaySFX(int index);
    void PlaySFX(string path);
    void SetBgmVolume(float v);
    void SetSfxVolume(float v);
    void Clear();
}

public interface IPoolManager
{
    void Init();
    void Push(Poolable poolable);
    Poolable Pop(GameObject original, Transform parent = null, int count = 5);
    void CreatePool(GameObject original, int count = 5);
    GameObject GetOriginal(string name);
    void Clear();
} 