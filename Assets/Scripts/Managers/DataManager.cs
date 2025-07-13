using JustClimb.Data;
using JustClimb.Manager;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

// 로컬 캐시와 서버 GET (Load)
// 로컬 JSON 저장(SaveLocal)
// 풀 덤프/필드 델타 발생(GenerateFullDelta/GenerateDelta)
public class DataManager : IDataManager, IInitializable
{
    // 실제 게임 플레이 중 read/write 하는 유저 저장 파일
    private readonly string _filePath;

    // 메모리 상에 올려둔 JSON 역직렬화 결과
    public SaveData Current { get; private set; }

    // 데이터 로드/저장 완료 콜백
    public event Action<SaveData> OnLoaded;
    public event Action<SaveData> OnSaved;
    public event Action<DeltaEvent> OnDeltaGenerated;  // 델타가 생성될 때마다 발생
    // Unity 메인 스레드로 이벤트를 전송하기 위한 컨텍스트
    readonly SynchronizationContext _syncCtx;
    readonly IDataSyncManager _syncMgr;

    // 서버 호출용 HttpClient
    readonly HttpClient _http = new HttpClient();
    readonly string _serverUrl;
    readonly string _userId;        // 스팀ID나 자체 유저ID
    readonly ServerConfig _serverConfig;
    readonly SteamAuthManager _steamAuthManager;  // Steam 인증 매니저

    // 파일 접근 동기화를 위한 세마포어
    private readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);

    [Inject]
    public DataManager(IDataSyncManager syncMgr, [Inject(Id = "UserId")] string userId, SteamAuthManager steamAuthManager)
    {
        _syncMgr = syncMgr;
        _userId = userId;
        _steamAuthManager = steamAuthManager;
        _syncCtx = SynchronizationContext.Current;
        _filePath = Path.Combine(Application.persistentDataPath, "save.json");

        // 서버 설정 로드
        _serverConfig = Resources.Load<ServerConfig>("ServerConfig");
        if (_serverConfig == null)
        {
            Debug.LogError("[DataManager] ServerConfig를 찾을 수 없습니다! Resources/ServerConfig.asset을 생성하세요.");
            _serverUrl = "https://localhost:7091/api/users";  // 기본값
        }
        else
        {
            _serverUrl = _serverConfig.GetUserStateApiUrl();
            // HTTP 클라이언트 타임아웃 설정
            _http.Timeout = TimeSpan.FromSeconds(_serverConfig.timeoutSeconds);
        }
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
        var loadTask = LoadAsync();
    }

    /// <summary>
    /// HTTP 요청에 JWT 토큰 헤더 추가
    /// </summary>
    private void AddAuthorizationHeader(HttpRequestMessage request)
    {
        if (_steamAuthManager != null && _steamAuthManager.HasValidToken())
        {
            request.Headers.Add("Authorization", $"Bearer {_steamAuthManager.JwtToken}");
        }
    }

    /// <summary>
    /// 리팩토링된 LoadAsync:
    /// 1) 오프라인일 때 로컬에 JSON으로 임시 로드(캐시)(GET)
    /// 2) 온라인일 때 서버에서 최신 JSON GET → 덮어쓰기 + OnLoaded 재발행
    /// </summary>
    async Task LoadAsync()
    {
        SaveData local = null;

        // 파일 접근 동기화
        await _fileSemaphore.WaitAsync();
        try
        {
            // 1) 로컬에서
            if (File.Exists(_filePath))
            {
                try
                {
                    var txt = await File.ReadAllTextAsync(_filePath);
                    local = JsonConvert.DeserializeObject<SaveData>(txt) ?? new SaveData();
                    
                    // 🔧 로컬 데이터도 정리 (null 값들을 기본값으로 대체)
                    CleanupServerData(local);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[DataManager] 로컬 파일 읽기 실패: {e.Message}");
                    local = new SaveData();
                }
            }
            else local = new SaveData();

            Current = local;
            _syncCtx.Post(_ => OnLoaded?.Invoke(Current), null);
        }
        finally
        {
            _fileSemaphore.Release();
        }

        // 2) 온라인이면 서버(DB)에서 최신 데이터를 불러와 사용 (GET)
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            try
            {
                var url = $"{_serverUrl}/{_userId}/state";
                
                // JWT 토큰을 포함한 HTTP 요청 생성
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    AddAuthorizationHeader(request);
                    
                    var response = await _http.SendAsync(request);
                    var json = await response.Content.ReadAsStringAsync();
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var serverData = JsonConvert.DeserializeObject<SaveData>(json) ?? new SaveData();
                        
                        // 🔧 서버 데이터 정리 (null 값들을 기본값으로 대체)
                        CleanupServerData(serverData);

                        // 덮어쓰기 + 로컬 저장 (파일 접근 동기화)
                        await _fileSemaphore.WaitAsync();
                        try
                        {
                            await File.WriteAllTextAsync(_filePath, json);
                            Current = serverData;
                            _syncCtx.Post(_ => OnLoaded?.Invoke(Current), null);
                        }
                        finally
                        {
                            _fileSemaphore.Release();
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[DataManager] 서버 로드 실패: {response.StatusCode} - {json}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DataManager] 서버 로드 실패: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 로컬 JSON에만 저장하고 OnSaved 호출
    /// </summary>
    public void SaveLocal()
    {
        _ = SaveLocalAsync();
    }

    async Task SaveLocalAsync()
    {
        // ✅ Newtonsoft.Json 사용 (enum 변환 지원)
        var json = JsonConvert.SerializeObject(Current, Formatting.Indented);

        // 파일 접근 동기화
        await _fileSemaphore.WaitAsync();
        try
        {
            await File.WriteAllTextAsync(_filePath, json);
            _syncCtx.Post(_ => OnSaved?.Invoke(Current), null);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] 로컬 저장 실패: {e.Message}");
        }
        finally
        {
            _fileSemaphore.Release();
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
        // ✅ Newtonsoft.Json 사용 (enum 변환 지원)
        var json = JsonConvert.SerializeObject(Current);
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
        string json;

        // 기본 타입과 복합 타입을 구분하여 직렬화
        if (val is int)
        {
            json = val.ToString();
        }
        else if (val is float floatVal)
        {
            json = floatVal.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        else if (val is bool boolVal)
        {
            json = boolVal ? "true" : "false";
        }
        else if (val is string)
        {
            json = val.ToString();
        }
        else if (IsListType(val))
        {
            // 리스트/배열은 Newtonsoft.Json으로 직렬화 (JsonUtility는 루트 레벨 배열 지원 안 함)
            json = JsonConvert.SerializeObject(val);
        }
        else
        {
            // ✅ 복합 타입도 Newtonsoft.Json으로 직렬화 (enum 변환 지원)
            json = JsonConvert.SerializeObject(val);
        }

        Debug.Log($"[DataManager] 델타 생성 - Key: {key}, Value: {val}, JSON: {json}");
        SyncDelta(new DeltaEvent(key, json));
    }

    /// <summary>
    /// 객체가 리스트/배열 타입인지 확인
    /// </summary>
    private bool IsListType(object obj)
    {
        if (obj == null) return false;

        var type = obj.GetType();
        return type.IsArray ||
               (type.IsGenericType &&
                typeof(IEnumerable).IsAssignableFrom(type) &&
                !typeof(string).IsAssignableFrom(type));
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

    /// <summary>
    /// 🔧 서버에서 받은 데이터의 null 값들을 기본값으로 정리
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
        data.achievementProgress.unlockedCharacters ??= new List<string>();
        data.achievementProgress.itemTypesUsed ??= new List<string>();

        Debug.Log($"[DataManager] 서버 데이터 정리 완료 - stageFlagPositions: {data.stageFlagPositions.Count}개");
    }
    // External: OfflineCacheManager 는 dataManager.OnDeltaGenerated += CacheDelta 로
    // 오프라인일 때 델타를 가로채 저장할 수 있고,
    // Internal: 바로 이어서 DataSyncManager.EnqueueDelta 가 호출되어
    // 온라인일 때는 즉시 서버에 전송하게 된다.

    // 메모리 누수 방지 (Zenject 싱글톤용 IDisposable 구현)
    public void Dispose()
    {
        // HTTP 클라이언트 정리
        _http?.Dispose();

        // 세마포어 정리
        _fileSemaphore?.Dispose();

        // 외부 구독자들이 남아있을 수 있으니 이벤트 초기화
        OnLoaded = null;
        OnSaved = null;
        OnDeltaGenerated = null;

        // 현재 데이터 정리
        Current = null;

        // 매니저 참조 해제 (readonly 필드는 해제할 수 없음)
        // _syncMgr, _userId, _syncCtx, _filePath, _serverUrl는 readonly이므로 제외
    }
}
