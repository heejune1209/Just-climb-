using JustClimb.Manager;
using Unity.VisualScripting;
using UnityEngine;


// Managers 컨테이너만 static Instance 로 남기고,
// 나머지 매니저들은 모두 Managers의 인스턴스 필드로 관리.
// Singleton Manager Container
public class Managers : MonoBehaviour
{
    // 오직 하나의 static 진입점만 유지

    private static Managers _instance;
    /// <summary>
    /// 외부에서 이 프로퍼티를 호출하면
    /// 1) 이미 Awake()에서 _instance가 세팅돼 있으면 그걸,
    /// 2) 씬에서 찾을 수 있으면 그걸,
    /// 3) 없으면 새로 만들어서 리턴.
    /// </summary>
    public static Managers Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            _instance = FindObjectOfType<Managers>();
            if (_instance != null)
                return _instance;

            var go = new GameObject("@Managers");
            _instance = go.AddComponent<Managers>();
            DontDestroyOnLoad(go);
            return _instance;
        }
    }

    // ───── Persistence Layer ─────
    /// <summary>로컬 JSON 저장/로드 담당</summary>
    public DataManager Data { get; private set; }

    // ───── Domain Layer ─────
    /// <summary>재화(골드,보석) 관리</summary>
    public CurrencyManager Currency { get; private set; }
    /// <summary>인벤토리,아이템 관리</summary>
    public ItemManager Item { get; private set; }
    /// <summary>스테이지 클리어,보상 관리</summary>
    public StageManager Stage { get; private set; }

    /// <summary>게임 전반의 흐름 관리 (MonoBehaviour)</summary>
    public GameManager Game { get; private set; }

    // ───── Infrastructure Layer ─────
    /// <summary>에셋 로딩 관리</summary>
    public ResourceManager Resource { get; private set; }
    /// <summary>UI 팝업·HUD 관리</summary>
    public UIManager UI { get; private set; }
    /// <summary>씬 전환 관리</summary>
    public SceneManagerEx Scene { get; private set; }
    /// <summary>BGM/SFX 관리 (MonoBehaviour)</summary>
    public SoundManager Sound { get; private set; }
    /// <summary>오브젝트 풀 관리</summary>
    public PoolManager Pool { get; private set; }
    /// <summary>ScriptableObject 기반 아이템 정의</summary>
    public ItemDatabase ItemDB { get; private set; }
    public RankingManager Ranking { get; private set; }

    void Awake()
    {
        // 이미 다른 인스턴스가 있으면 파괴
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 2) 이 Awake에서 최초 단 한 번만 _instance 세팅
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 1) Persistence
        Data = new DataManager();
        Data.Init();

        // 2) Infrastructure: Resource → Pool 먼저
        Resource = new ResourceManager();
        Pool = new PoolManager();
        Pool.Init();

        UI = new UIManager();
        UI.Init();
        Scene = new SceneManagerEx();

        ItemDB = new ItemDatabase();
        ItemDB.Init();

        // 3) Domain
        Currency = new CurrencyManager();
        Currency.Init();

        Item = GetOrAddComponent<ItemManager>();
        Item.Init();

        Stage = new StageManager();
        Stage.Init();

        Game = GetOrAddComponent<GameManager>();
        Game.Init();

        Ranking = new RankingManager();
        Ranking.Init();

        // Resources 폴더 기준으로 Managers/SoundManager 를 찾습니다
        var go = Resource.Instantiate("Managers/SoundManager");
        if (go == null)
        {
            Debug.LogError("[Managers] SoundManager prefab을 Resources/Managers/SoundManager.prefab 에서 찾을 수 없습니다.");
        }
        else
        {
            DontDestroyOnLoad(go);
            Sound = go.GetComponent<SoundManager>();
            if (Sound == null)
                Debug.LogError("[Managers] SoundManager prefab에 SoundManager 컴포넌트가 없습니다.");
            else
                Sound.Init();
        }


    }

    /// <summary>
    /// MonoBehaviour 타입 매니저를 "@Managers" 오브젝트에 붙여두고,
    /// 이미 있으면 가져오는 헬퍼 함수.
    /// </summary>
    T GetOrAddComponent<T>() where T : MonoBehaviour
    {
        var comp = GetComponent<T>();
        if (comp == null)
            comp = gameObject.AddComponent<T>();
        return comp;
    }

    public static void Clear()
    {
        //Sound.Clear();
        Instance.UI.ClearPopupUI();
        Instance.Pool.Clear();
    }
}
