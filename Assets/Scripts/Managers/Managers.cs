using UnityEngine;


// 게임 매니저로서 동작할 스크립트
public class Managers : MonoBehaviour
{
    // 싱글톤 구현하기
    // 게임 매니저는 게임 전반을 관리하기 때문에 게임 매니저 스크립트는 딱 하나만 static으로 만들어 두고 여러 곳에서 이 동일한 인스턴스를 참조한다.
    // 게임 매니저 오브젝트의 스크립트 컴포넌트를 여러 곳에서 공유할 수 있도록 static 으로 만들어 둔다.
    // 단 하나만 존재하는 이 게임 매니저 컴포넌트를 리턴 받을 수 있는 static 함수를 만들어 둔다.
    // 다른 곳에서 사용할 수 있도록 public이어야 한다.

    static Managers s_Instance;  // 유일성이 보장된다.
    static Managers Instance // 유일한 매니저를 갖고 온다. GetInstance() 함수를 프로퍼티로 바꿔주었다.
    {
        get
        {
            Init();
            return s_Instance;
        }
    }
    // 싱글톤으로 만들 대상은 “@Managers” 오브젝트에 붙어 있는 📜Managers.cs
    // 여러 곳에서 동일한 인스턴스에 대해 공유할 수 있도록 static으로 선언한다.
    // static Managers Instance
    // 여러 곳에서 동일한 인스턴스를 리턴받아 사용할 수 있도록 이 인스턴스를 리턴하는 static public 함수를 만들어둔다.
    // 클래스 함수가 되므로 다른 여러 곳에서 클래스이름.GetInstance() 로 사용할 수 있게 된다.
    // Instance에 📜Managers.cs 할당하는 것을 딱 한군데에서만 해주면 단 하나만 존재하는게 보장된다.

    
    // Managers.cs 인스턴스(Instance)는 싱글톤.
    // Managers.cs 안에서 InputManager.cs 타입의 _input 인스턴스를 생성했으니 이제 _input는 Managers.cs의 멤버가 된다.
    // 따라서 _input이 Managers.cs에 속한 멤버이니 InputManager.cs 도 싱글톤인게 보장된다.
    // Input는 이 Managers.cs 인스턴스의 _input을 불러오는 프로퍼티다. (싱글톤 불러다 주는 static 함수 같은 역할.프로퍼티로 구현한 것 뿐.)

    // 📜Resource Manager도 📜Manager.cs 의 멤버로 두어(구성 요소로 두어) 싱글톤으로 관리하자.
    // 기존 매니저들...
    public static ResourceManager Resource { get { return Instance._resource; } }
    public static UIManager UI { get { return Instance._ui; } }
    public static SceneManagerEx Scene { get { return Instance._scene; } }
    public static SoundManager Sound
    {
        get { return Instance._sound; }
        set { Instance._sound = value; } // ← 여기 set 추가
    }
    public static PoolManager Pool { get { return Instance._pool; } }
    public static DataManager Data { get { return Instance._data; } }

    public static GameManager Game
    {
        get
        {
            if (Instance._game == null)
            {
                // 씬에 붙어 있는 GameManager 탐색
                var gm = FindObjectOfType<GameManager>();
                if (gm == null)
                {
                    // 없으면 새로 생성
                    var go = new GameObject { name = "@GameManagers" };
                    gm = go.AddComponent<GameManager>();
                }
                Instance._game = gm;
            }
            return Instance._game;
        }
    }

    ResourceManager _resource = new ResourceManager();
    UIManager _ui = new UIManager();
    SceneManagerEx _scene = new SceneManagerEx();
    SoundManager _sound;         // ← MonoBehaviour 컴포넌트로 받을 것
    PoolManager _pool = new PoolManager();   
    DataManager _data = new DataManager();
    // GameManager 인스턴스를 담을 필드 (MonoBehaviour이므로 new 하지 않습니다)
    GameManager _game;




    void Start()
    {
        Init();
    }

    // Update() 유니티 이벤트 함수
    // 매프레임마다 _input.OnUpdate()을 실행시켜주기만 하면 땡이다.
    // 📜InputManager.cs의 OnUpdate() 함수
    // 여기서는 또 KeyAction만 Invoke 시키는 일을 한다.
    void Update()
    {
        //_input.OnUpdate();
    }

    static void Init()
    {
        // GetInstance(), Start() 두 함수에서 Init()을 호출하는 이유
        // Managers.cs의 Start() 함수가 실행되기도 전에 다른 스크립트에서 게임 매니저 Instance를 사용해야 할 일이 생긴다면 먼저 그 곳에서 Instance를 만들어 두도록 하기 위하여.

        // Instance가 아직 없다면 만들자. 있다면 다음과 같은 과정을 무시하고 지나가면 된다. 👉 if (Instance == null)
        // Instance가 단 하나만 존재할 수 있도록 보장이 된다.

        // Instance 만들기
        // @Managers라는 오브젝트가 없다면 👉 if (obj == null)
        // 직접 만들고 Managers.cs 컴포넌트도 붙여주자.
        if (s_Instance == null)
        {
            GameObject obj = GameObject.Find("@Managers");
            if (obj == null)
            {
                obj = new GameObject { name = "@Managers" };
                obj.AddComponent<Managers>(); // Instance에 스크립트를 할당하는 곳
            }
            // “@Managers” 오브젝트가 씬이 변경되도 삭제되지 않고 유지 되도록 안전 장치를 걸어주자. 👉 DontDestroyOnLoad(obj)
            DontDestroyOnLoad(obj);
            // Instance에 “@Managers” 오브젝트에 붙어 있는 Managers.cs 가져오기
            s_Instance = obj.GetComponent<Managers>();

            // 플레이 시작하자마자 바로 생성
            var _ = Game;
          
            //s_Instance._data.Init();

            s_Instance._pool.Init(); 
        }
    }

    public static void Clear()
    { 
        //Sound.Clear();
        Scene.Clear();
        UI.Clear();
        Pool.Clear();
    }
}
