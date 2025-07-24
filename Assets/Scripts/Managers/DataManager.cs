using JustClimb.Data;
using JustClimb.Manager;
using JustClimb.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Zenject;

// 로컬 캐시와 서버 GET (Load)
// 로컬 JSON 저장(SaveLocal)
// 풀 덤프/필드 델타 발생(GenerateFullDelta/GenerateDelta)
public class DataManager : MonoBehaviour, IDataManager, IInitializable
{
    // 실제 게임 플레이 중 read/write 하는 유저 저장 파일
    private readonly string _filePath;

    // 메모리 상에 올려둔 JSON 역직렬화 결과
    public SaveData Current { get; private set; }

    // 데이터 로드/저장 완료 콜백
    public event Action<SaveData> OnLoaded;
    public event Action<SaveData> OnSaved;
    public event Action<DeltaEvent> OnDeltaGenerated;  // 델타가 생성될 때마다 발생
    // 서버 통신용 DataSyncManager (통합된 네트워크 레이어)
    readonly IDataSyncManager _syncMgr;
    readonly string _userId;        // 스팀ID나 자체 유저ID
    readonly ServerConfig _serverConfig;

    [Inject]
    public DataManager(IDataSyncManager syncMgr, [Inject(Id = "UserId")] string userId)
    {
        _syncMgr = syncMgr;
        _userId = userId;
        _filePath = Path.Combine(Application.persistentDataPath, "save.json");

        // ConfigHelper 사용 (중복 제거)
        _serverConfig = ConfigHelper.GetServerConfig();
    }

    /// <summary>
    /// Zenject에서 자동으로 호출됨.
    /// </summary>
    public void Initialize()
    {
        // 앱 시작 시 자동 Load 트리거
        Load();
    }

    /// <summary>
    /// 1) 로컬 JSON 로드 → OnLoaded
    /// 2) 온라인이면 GET → 덮어쓰기 → OnLoaded
    /// </summary>
    public void Load()
    {
        StartCoroutine(LoadCoroutine());
    }

    // JWT 토큰 헤더 추가는 DataSyncManager에서 자동으로 처리됨

    /// <summary>
    /// 리팩토링된 LoadCoroutine:
    /// 1) 로컬에서 JSON 로드 → OnLoaded
    /// 2) 온라인이면 서버에서 최신 JSON GET → 덮어쓰기 + OnLoaded 재발행
    /// DataSyncManager를 통한 통합 네트워크 통신 사용
    /// </summary>
    private IEnumerator LoadCoroutine()
    {
        SaveData local = null;

        // 1) 로컬에서 로드
        if (File.Exists(_filePath))
        {
            try
            {
                string txt = File.ReadAllText(_filePath);
                local = JsonHelper.DeserializeObject<SaveData>(txt, new SaveData());
                
                // 로컬 데이터도 정리 (null 값들을 기본값으로 대체)
                CleanupServerData(local);
                Debug.Log("[DataManager] 로컬 파일에서 데이터 로드 성공");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DataManager] 로컬 파일 읽기 실패: {e.Message}");
                local = new SaveData();
            }
        }
        else local = new SaveData();

        Current = local;
        OnLoaded?.Invoke(Current);

        // 2) 온라인이면 서버(DB)에서 최신 데이터를 불러와 사용 (GET)
        if (NetworkHelper.IsOnline())
        {
            var url = ConfigHelper.GetUserStateApiUrl(_userId);
            
            bool serverLoadCompleted = false;
            
            // DataSyncManager를 통한 통합 네트워크 통신
            yield return _syncMgr.StartNetworkCoroutine(_syncMgr.GetRequest<SaveData>(
                url,
                onSuccess: (serverData) => {
                    // 서버 데이터 정리 (null 값들을 기본값으로 대체)
                    CleanupServerData(serverData);
                    
                    // 덮어쓰기 + 로컬 저장
                    try
                    {
                        string json = JsonHelper.SerializeSaveData(serverData);
                        File.WriteAllText(_filePath, json);
                        Current = serverData;
                        OnLoaded?.Invoke(Current);
                        Debug.Log("[DataManager] 서버에서 데이터 로드 성공");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[DataManager] 서버 데이터 로컬 저장 실패: {e.Message}");
                    }
                    serverLoadCompleted = true;
                },
                onError: (error) => {
                    Debug.LogWarning($"[DataManager] 서버 로드 실패: {error}");
                    serverLoadCompleted = true;
                },
                defaultValue: new SaveData()
            ));
            
            // 서버 로드 완료 대기
            yield return new WaitUntil(() => serverLoadCompleted);
        }
    }

    /// <summary>
    /// 로컬 JSON에만 저장하고 OnSaved 호출
    /// </summary>
    public void SaveLocal()
    {
        try
        {
            //  JsonHelper 사용 (통합된 오류 처리)
            var json = JsonHelper.SerializeSaveData(Current);
            
            File.WriteAllText(_filePath, json);
            OnSaved?.Invoke(Current);
            
            Debug.Log("[DataManager] 로컬 저장 성공");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] 로컬 저장 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 풀 상태 덤프 + 로컬 저장
    /// 여러 필드가 한꺼번에 대규모로 바뀌거나 중요한 시점(레벨 시작·종료·버전업데이트 등)에 
    /// 전체 상태를 확실히 동기화하고 싶을 때 사용
    /// </summary>
    public void Save()
    {
        SaveLocal();
        GenerateFullDelta();
    }

    /// <summary>전체 상태 덤프 델타만 발생</summary>
    public void GenerateFullDelta()
    {
        // JsonHelper 사용 (통합된 오류 처리)
        var json = JsonHelper.SerializeObject(Current);
        Debug.Log($"[DataManager] 풀 델타 생성 - 크기: {json.Length} bytes");
        SyncDelta(new DeltaEvent("json:full", json));
    }

    /// <summary>
    /// 특정 필드 증분 델타만 발생
    /// 온라인일 때 변경된 델타를 즉시 서버에 저장(UPSERT)
    /// DataManager.GenerateDelta(...) → DataSyncManager.SyncLoop() → Flush()
    /// </summary>
    public void GenerateDelta(string key, object val)
    {
        // JsonHelper 사용 (타입 구분 로직 통합)
        string json = JsonHelper.SerializeDeltaValue(val);
        
        Debug.Log($"[DataManager] 델타 생성 - Key: {key}, Value: {val}, JSON: {json}");
        SyncDelta(new DeltaEvent(key, json));
    }

    /// <summary>델타 전송 진입점(단일)</summary>
    void SyncDelta(DeltaEvent d)
    {
        Debug.Log($"[DataManager] 델타 동기화 시작 - {d}");

        // 오프라인 캐싱 등 외부에서 필요한 로직이 이 이벤트를 통해 구독.
        OnDeltaGenerated?.Invoke(d);

        // 실제 서버 전송은 DataSyncManager로 위임
        _syncMgr.EnqueueDelta(d);
    }

    #region 도메인별 서버 통신 API (다른 매니저들을 위한 캡슐화된 인터페이스)

    /// <summary>
    /// 랭킹 데이터 조회 (RankingManager용)
    /// </summary>
    public void GetRanking<T>(int stageNum, int sortType, int page, int pageSize, Action<T> onSuccess, Action<string> onError, T defaultValue = default)
    {
        var url = $"{ConfigHelper.GetRankingApiUrl()}?stageNumber={stageNum}&sortType={sortType}&page={page}&pageSize={pageSize}&userId={_userId}";
        StartCoroutine(_syncMgr.GetRequest(url, onSuccess, onError, defaultValue));
    }

    /// <summary>
    /// 사용자 기록 업데이트 (RankingManager용)
    /// </summary>
    public void UpdateUserRecord<T>(object recordData, Action<T> onSuccess, Action<string> onError, T defaultValue = default)
    {
        var url = ConfigHelper.GetUserRecordApiUrl(_userId);
        StartCoroutine(_syncMgr.PostRequest(url, recordData, onSuccess, onError, defaultValue));
    }

    /// <summary>
    /// Steam 인증 (SteamAuthManager용)
    /// </summary>
    public void AuthenticateWithSteam<T>(object authData, Action<T> onSuccess, Action<string> onError, T defaultValue = default)
    {
        var url = ConfigHelper.GetSteamAuthApiUrl();
        StartCoroutine(_syncMgr.PostRequest(url, authData, onSuccess, onError, defaultValue));
    }

    /// <summary>
    /// 업적 데이터 조회 (AchievementManager용)
    /// </summary>
    public void GetAchievements<T>(Action<T> onSuccess, Action<string> onError, T defaultValue = default)
    {
        var url = $"{ConfigHelper.GetBaseUrl()}/api/achievements/{_userId}";
        StartCoroutine(_syncMgr.GetRequest(url, onSuccess, onError, defaultValue));
    }

    /// <summary>
    /// 업적 보상 수령 (AchievementManager용)
    /// </summary>
    public void ClaimAchievementReward<T>(string achievementId, Action<T> onSuccess, Action<string> onError, T defaultValue = default)
    {
        var url = $"{ConfigHelper.GetBaseUrl()}/api/achievements/{_userId}/{achievementId}/claim";
        StartCoroutine(_syncMgr.PostRequest(url, new { }, onSuccess, onError, defaultValue));
    }

    #endregion

    /// <summary>
    /// 서버에서 받은 데이터의 null 값들을 기본값으로 정리
    /// </summary>
    private void CleanupServerData(SaveData data)
    {
        if (data == null) return;

        // stageFlagPositions의 null 항목들을 기본값으로 대체
        if (data.stageFlagPositions != null)
        {
            for (int i = 0; i < data.stageFlagPositions.Count; i++)
            {
                if (data.stageFlagPositions[i] == null)
                {
                    data.stageFlagPositions[i] = new SerializableVector3Dto(0f, 0f, 0f);
                    Debug.Log($"[DataManager] stageFlagPositions[{i}] null 값을 기본값으로 대체");
                }
            }
        }
        else
        {
            data.stageFlagPositions = new List<SerializableVector3Dto>();
        }

        // 다른 리스트들과 객체들도 null 체크
        data.stageClears ??= new List<bool>();
        data.bestGemRewards ??= new List<int>();
        data.bestClearTimes ??= new List<float>();
        data.bestDeathCounts ??= new List<int>();
        data.currentPlayTimes ??= new List<float>();
        data.currentDeathCounts ??= new List<int>();
        data.items ??= new List<InventoryItemDto>();
        data.achievementRewards ??= new Dictionary<string, bool>();
        data.achievementUnlocked ??= new Dictionary<string, bool>();
        
        // AchievementProgressDto 내부도 null 체크
        if (data.achievementProgress == null)
        {
            data.achievementProgress = new AchievementProgressDto();
        }
        
        // AchievementProgressDto의 리스트들도 null 체크
        data.achievementProgress.unlockedCharacters ??= new List<int>();
        data.achievementProgress.itemTypesUsed ??= new List<string>();
        data.achievementProgress.chapter1PerfectStages ??= new List<int>();

        Debug.Log($"[DataManager] 서버 데이터 정리 완료 - stageFlagPositions: {data.stageFlagPositions.Count}개");
    }
    // External: OfflineCacheManager 는 dataManager.OnDeltaGenerated += CacheDelta 로
    // 오프라인일 때 델타를 가로채 저장할 수 있고,
    // Internal: 바로 이어서 DataSyncManager.EnqueueDelta 가 호출되어
    // 온라인일 때는 즉시 서버에 전송하게 된다.

    // 메모리 누수 방지 (Zenject 싱글톤용 IDisposable 구현)
    public void Dispose()
    {
        // 외부 구독자들이 남아있을 수 있으니 이벤트 초기화
        OnLoaded = null;
        OnSaved = null;
        OnDeltaGenerated = null;

        // 현재 데이터 정리
        Current = null;
        
        Debug.Log("[DataManager] 리소스 정리 완료 (HttpClient, SemaphoreSlim 제거됨)");
    }
}
