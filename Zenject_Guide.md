# Zenject DI 패턴 적용 가이드

## 1. 개요
이 프로젝트는 기존의 싱글톤 패턴(`Managers.Instance`)에서 Zenject DI(Dependency Injection) 패턴으로 전환되었습니다.

## 2. 주요 변경사항

### 2.1 이전 방식 (싱글톤)
```csharp
// ❌ 더 이상 사용하지 마세요
var gold = Managers.Instance.Currency.GetGold();
Managers.Instance.UI.ShowPopupUI<UI_Shop>();
```

### 2.2 새로운 방식 (DI)
```csharp
public class ShopController : MonoBehaviour
{
    [Inject] private ICurrencyManager _currencyManager;
    [Inject] private IUIManager _uiManager;
    
    void ShowShop()
    {
        var gold = _currencyManager.GetGold();
        _uiManager.ShowPopupUI<UI_Shop>();
    }
}
```

## 3. 매니저 인터페이스 목록

| 인터페이스 | 구현체 | 설명 |
|-----------|--------|------|
| `IDataManager` | `DataManager` | 게임 데이터 저장/로드 |
| `ICurrencyManager` | `CurrencyManager` | 재화(골드, 젬) 관리 |
| `IItemManager` | `ItemManager` | 아이템 인벤토리 관리 |
| `IStageManager` | `StageManager` | 스테이지 진행 관리 |
| `IGameManager` | `GameManager` | 게임 플로우 관리 |
| `IRankingManager` | `RankingManager` | 랭킹 시스템 |
| `IResourceManager` | `ResourceManager` | 리소스 로딩 |
| `IUIManager` | `UIManager` | UI 관리 |
| `ISceneManagerEx` | `SceneManagerEx` | 씬 전환 |
| `ISoundManager` | `SoundManager` | 사운드 재생 |
| `IPoolManager` | `PoolManager` | 오브젝트 풀링 |

## 4. 사용 방법

### 4.1 MonoBehaviour에서 사용
```csharp
public class PlayerController : MonoBehaviour
{
    [Inject] private IGameManager _gameManager;
    [Inject] private ISoundManager _soundManager;
    
    void OnPlayerDeath()
    {
        _gameManager.OnPlayerDead();
        _soundManager.PlaySFX("death");
    }
}
```

### 4.2 일반 클래스에서 사용
```csharp
public class ScoreCalculator
{
    private readonly IStageManager _stageManager;
    private readonly ICurrencyManager _currencyManager;
    
    public ScoreCalculator(IStageManager stageManager, ICurrencyManager currencyManager)
    {
        _stageManager = stageManager;
        _currencyManager = currencyManager;
    }
    
    public int CalculateTotalScore()
    {
        // 생성자 주입된 매니저 사용
        return _stageManager.GetBestReward(1) + _currencyManager.GetGold();
    }
}
```

## 5. 프로젝트 설정

### 5.1 ProjectContext 설정
1. Unity 에디터에서 `Resources/ProjectContext` 프리팹을 찾습니다
2. Inspector에서 `Mono Installers` 리스트에 `ProjectInstaller`가 추가되어 있는지 확인합니다
3. 없다면 `+` 버튼을 클릭하고 `ProjectInstaller`를 선택합니다

### 5.2 씬별 설정 (선택사항)
특정 씬에서만 사용하는 의존성이 있다면 SceneContext를 추가할 수 있습니다:
1. 씬에 빈 GameObject 생성
2. `Zenject > Scene Context` 컴포넌트 추가
3. 씬 전용 Installer 작성 및 연결

## 6. 마이그레이션 가이드

### 6.1 기존 코드 수정
```csharp
// 이전 코드
void Start()
{
    Managers.Instance.Data.Load();
    Managers.Instance.Currency.AddGold(100);
}

// 새 코드
[Inject] private IDataManager _dataManager;
[Inject] private ICurrencyManager _currencyManager;

void Start()
{
    _dataManager.Load();
    _currencyManager.AddGold(100);
}
```

### 6.2 테스트 코드 작성
```csharp
[Test]
public void TestGoldAddition()
{
    // Mock 객체 생성
    var mockCurrency = Substitute.For<ICurrencyManager>();
    mockCurrency.GetGold().Returns(100);
    
    // 테스트 실행
    var calculator = new ScoreCalculator(null, mockCurrency);
    Assert.AreEqual(100, calculator.GetGoldScore());
}
```

## 7. 주의사항

1. **초기화 순서**: Zenject가 자동으로 의존성을 주입하므로 `Awake()`에서 매니저를 사용하지 마세요. `Start()` 이후에 사용하세요.

2. **순환 참조**: A가 B를 주입받고, B가 A를 주입받는 순환 참조를 피하세요.

3. **인터페이스 사용**: 구체 타입 대신 인터페이스를 주입받아 테스트와 확장성을 높이세요.

4. **NonLazy 바인딩**: `ProjectInstaller`에서 `NonLazy()`를 사용하여 앱 시작 시 모든 매니저가 초기화되도록 했습니다.

## 8. 문제 해결

### "NullReferenceException" 발생
- ProjectContext가 씬에 있는지 확인
- 해당 매니저가 ProjectInstaller에 바인딩되어 있는지 확인
- `[Inject]` 어트리뷰트가 제대로 선언되어 있는지 확인

### "Cannot resolve type" 에러
- 인터페이스와 구현체가 올바르게 바인딩되어 있는지 확인
- 네임스페이스가 올바른지 확인

### 기존 코드와의 호환성
- `Managers.cs`는 deprecated 되었지만 임시로 남겨두었습니다
- 점진적으로 마이그레이션하면서 모든 참조가 제거되면 삭제 예정입니다 

`NonLazy()`는 Zenject에서 **즉시 초기화**를 강제하는 기능입니다.

## 🔄 Lazy vs NonLazy

### **기본 동작 (Lazy)**
```csharp
Container.Bind<IDataManager>().To<DataManager>().AsSingle();
// DataManager는 누군가 처음 요청할 때까지 생성되지 않음
```

### **NonLazy 동작**
```csharp
Container.Bind<IDataManager>().To<DataManager>().AsSingle().NonLazy();
// 앱 시작과 동시에 DataManager가 즉시 생성됨
```

## 📋 구체적인 차이점

### 1. **Lazy (기본값)**
```csharp
public class PlayerController : MonoBehaviour
{
    [Inject] private IDataManager _dataManager;
    
    void Start()
    {
        // 이 시점에서 DataManager가 처음 생성됨
        _dataManager.Load();
    }
}
```

### 2. **NonLazy**
```csharp
// 앱 시작 시 ProjectContext가 로드되는 순간
// DataManager가 즉시 생성되고 Init() 호출됨
```

## 🎯 언제 NonLazy를 사용하나요?

### ✅ **NonLazy가 필요한 경우**
1. **초기화가 중요한 매니저들**
   ```csharp
   // 게임 시작과 동시에 데이터를 로드해야 함
   Container.Bind<IDataManager>().To<DataManager>().AsSingle().NonLazy();
   
   // 사운드 시스템을 미리 준비해야 함
   Container.Bind<ISoundManager>().To<SoundManager>().AsSingle().NonLazy();
   ```

2. **이벤트 구독이 필요한 매니저들**
   ```csharp
   public class DataSyncManager : MonoBehaviour
   {
       [Inject]
       public void Construct(IDataManager dataManager)
       {
           // 앱 시작과 동시에 데이터 변경 이벤트를 구독해야 함
           dataManager.OnDeltaGenerated += HandleDelta;
       }
   }
   ```

### ❌ **Lazy가 적합한 경우**
```csharp
// UI는 실제로 필요할 때만 생성
Container.Bind<IShopUI>().To<ShopUI>().AsTransient();

// 특정 스테이지에서만 사용하는 기능
Container.Bind<IBossAI>().To<BossAI>().AsSingle();
```

## 🚀 프로젝트에서의 활용

```csharp
public override void InstallBindings()
{
    // 핵심 매니저들 - 앱 시작과 동시에 초기화
    Container.Bind<IDataManager>().To<DataManager>().AsSingle().NonLazy();
    Container.Bind<ISoundManager>().To<SoundManager>().AsSingle().NonLazy();
    Container.Bind<IPoolManager>().To<PoolManager>().AsSingle().NonLazy();
    
    // 필요할 때만 생성되는 것들
    Container.Bind<IShopService>().To<ShopService>().AsSingle(); // Lazy
}
```

## ⚡ 성능 고려사항

- **NonLazy**: 앱 시작 시간이 약간 늘어날 수 있지만, 런타임에서 빠름
- **Lazy**: 앱 시작은 빠르지만, 첫 사용 시 약간의 지연 발생

결론적으로 `NonLazy()`는 **"앱 시작과 동시에 반드시 준비되어야 하는 핵심 시스템들"**에 사용하는 기능입니다! 🎯

`FromNewComponentOnNewGameObject()`는 Zenject에서 **MonoBehaviour 컴포넌트를 새로운 GameObject에 추가해서 생성**하는 바인딩 방법입니다.

## 🎯 기본 개념

### **일반 클래스 vs MonoBehaviour**
```csharp
// ✅ 일반 C# 클래스 - 그냥 new로 생성 가능
Container.Bind<IDataManager>().To<DataManager>().AsSingle();

// ❌ MonoBehaviour - new로 생성할 수 없음!
// Container.Bind<IItemManager>().To<ItemManager>().AsSingle(); // 에러!

// ✅ MonoBehaviour - GameObject에 컴포넌트로 추가해야 함
Container.Bind<IItemManager>().To<ItemManager>()
    .FromNewComponentOnNewGameObject().AsSingle();
```

## 🔧 다양한 MonoBehaviour 바인딩 방법

### 1. **FromNewComponentOnNewGameObject()**
```csharp
Container.Bind<IItemManager>().To<ItemManager>()
    .FromNewComponentOnNewGameObject().AsSingle();

// 결과: 새 GameObject "[ItemManager]"가 생성되고, ItemManager 컴포넌트가 추가됨
```

### 2. **FromComponentInNewPrefab()**
```csharp
Container.Bind<ISoundManager>().To<SoundManager>()
    .FromComponentInNewPrefab(soundManagerPrefab).AsSingle();

// 결과: 프리팹을 인스턴스화하고, 그 안의 SoundManager 컴포넌트를 사용
```

### 3. **FromComponentInNewPrefabResource()**
```csharp
Container.Bind<ISoundManager>().To<SoundManager>()
    .FromComponentInNewPrefabResource("Managers/SoundManager").AsSingle();

// 결과: Resources 폴더에서 프리팹을 로드해서 인스턴스화
```

### 4. **FromComponentInHierarchy()**
```csharp
Container.Bind<IUIManager>().To<UIManager>()
    .FromComponentInHierarchy().AsSingle();

// 결과: 씬에 이미 있는 UIManager 컴포넌트를 찾아서 사용
```

## 🎮 프로젝트에서의 실제 사용

```csharp
public override void InstallBindings()
{
    // 일반 클래스들 - 단순 생성
    Container.Bind<IDataManager>().To<DataManager>().AsSingle().NonLazy();
    Container.Bind<ICurrencyManager>().To<CurrencyManager>().AsSingle().NonLazy();
    Container.Bind<IStageManager>().To<StageManager>().AsSingle().NonLazy();
    
    // MonoBehaviour들 - GameObject에 컴포넌트로 추가
    Container.Bind<IItemManager>().To<ItemManager>()
        .FromNewComponentOnNewGameObject().AsSingle().NonLazy();
        
    Container.Bind<IGameManager>().To<GameManager>()
        .FromNewComponentOnNewGameObject().AsSingle().NonLazy();
        
    Container.Bind<IDataSyncManager>().To<DataSyncManager>()
        .FromNewComponentOnNewGameObject().AsSingle().NonLazy();
    
    // 프리팹에서 로드
    Container.Bind<ISoundManager>().To<SoundManager>()
        .FromComponentInNewPrefabResource("Managers/SoundManager")
        .AsSingle().NonLazy();
}
```

## 🏗️ 생성되는 구조

```
DontDestroyOnLoad
├── [ProjectContext]
├── [ItemManager]           ← FromNewComponentOnNewGameObject
├── [GameManager]           ← FromNewComponentOnNewGameObject  
├── [DataSyncManager]       ← FromNewComponentOnNewGameObject
└── SoundManager(Clone)     ← FromComponentInNewPrefabResource
```

## ⚠️ 주의사항

### **왜 MonoBehaviour는 특별한 처리가 필요한가?**
```csharp
// ❌ 이렇게 하면 안됨!
var itemManager = new ItemManager(); // MonoBehaviour는 new로 생성 불가!

// ✅ Unity에서는 이렇게 해야 함
var go = new GameObject("ItemManager");
var itemManager = go.AddComponent<ItemManager>();
```

### **생명주기 관리**
```csharp
// Zenject가 자동으로 DontDestroyOnLoad 처리
Container.Bind<IItemManager>().To<ItemManager>()
    .FromNewComponentOnNewGameObject().AsSingle();
    
// GameObject가 씬 전환 시에도 유지됨
```

## 🎯 언제 사용하나요?

- **MonoBehaviour를 상속받은 매니저들**
- **Unity의 생명주기 메서드가 필요한 경우** (Update, Coroutine 등)
- **Inspector에서 설정값을 조정하고 싶은 경우**

결론적으로 `FromNewComponentOnNewGameObject()`는 **"MonoBehaviour 매니저를 DI 컨테이너에서 관리하기 위한 필수 방법"**입니다! 🎮

---

네, 맞습니다! `IInitializable`은 **Zenject에서 제공하는 초기화 인터페이스**입니다.

## 🔧 IInitializable의 역할

### **기본 개념**
```csharp
public interface IInitializable
{
    void Initialize();
}
```

`IInitializable`을 구현한 클래스는 **Zenject 컨테이너가 모든 의존성 주입을 완료한 후 자동으로 `Initialize()` 메서드를 호출**해줍니다.

## 📋 실행 순서

### **1. 일반적인 생성자 주입**
```csharp
public class CurrencyManager : ICurrencyManager
{
    private readonly IDataManager _dataManager;
    
    public CurrencyManager(IDataManager dataManager)
    {
        _dataManager = dataManager;
        // 이 시점에서는 아직 다른 매니저들이 완전히 초기화되지 않을 수 있음
    }
}
```

### **2. IInitializable 사용**
```csharp
public class CurrencyManager : ICurrencyManager, IInitializable
{
    private readonly IDataManager _dataManager;
    
    public CurrencyManager(IDataManager dataManager)
    {
        _dataManager = dataManager;
        // 의존성 주입만 완료
    }
    
    public void Initialize()
    {
        // 모든 의존성이 완전히 준비된 후 호출됨
        _dataManager.OnLoaded += UpdateCurrencies;
        UpdateCurrencies(_dataManager.Current);
    }
}
```

## ⚡ 실행 흐름

```
1. Zenject 컨테이너 시작
   ↓
2. 모든 객체 생성 (생성자 호출)
   ↓
3. 의존성 주입 완료
   ↓
4. IInitializable.Initialize() 자동 호출 ← 여기서 초기화!
   ↓
5. 애플리케이션 실행
```

## 🎯 왜 필요한가요?

### **문제 상황**
```csharp
public class RankingManager
{
    public RankingManager(IDataManager dataManager, IStageManager stageManager)
    {
        // 이 시점에서 StageManager.Init()이 아직 호출되지 않았을 수 있음
        _stageManager.OnBestTimeUpdated += UpdateRanking; // 위험!
    }
}
```

### **해결책**
```csharp
public class RankingManager : IRankingManager, IInitializable
{
    public RankingManager(IDataManager dataManager, IStageManager stageManager)
    {
        // 의존성만 저장
        _dataManager = dataManager;
        _stageManager = stageManager;
    }
    
    public void Initialize()
    {
        // 모든 매니저가 준비된 후 이벤트 구독
        _stageManager.OnBestTimeUpdated += UpdateRanking; // 안전!
    }
}
```

## 🔄 ProjectInstaller에서의 설정

```csharp
public override void InstallBindings()
{
    Container.Bind<IDataManager>().To<DataManager>().AsSingle().NonLazy();
    Container.BindInterfacesTo<DataManager>().AsSingle().NonLazy();
    //                    ↑
    // 이 부분이 IInitializable도 함께 바인딩해서 자동 호출되도록 함
}
```

## 📊 다른 Zenject 생명주기 인터페이스들

```csharp
public interface IInitializable
{
    void Initialize(); // 시작 시 한 번 호출
}

public interface ITickable
{
    void Tick(); // 매 프레임 호출 (Update와 비슷)
}

public interface IDisposable
{
    void Dispose(); // 종료 시 호출
}
```

## 🎯 결론

`IInitializable`은 **"모든 의존성이 준비된 후 안전하게 초기화하고 싶을 때"** 사용하는 Zenject의 핵심 기능입니다. 

특히 **이벤트 구독, 초기 데이터 로드, 매니저 간 연결** 등을 할 때 매우 유용합니다! 🚀

좋은 질문입니다! Unity의 `Awake()`/`Start()`와 Zenject의 `IInitializable.Initialize()`는 서로 다른 목적과 실행 시점을 가지고 있습니다.

## 🔄 실행 순서 비교

### **Unity 생명주기**
```
1. Awake() - 객체 생성 직후
2. Start() - 첫 번째 프레임 전
3. Update() - 매 프레임
```

### **Zenject + Unity 생명주기**
```
1. 생성자 호출 (의존성 주입)
2. Awake() - Unity 생명주기
3. IInitializable.Initialize() - Zenject 초기화
4. Start() - Unity 생명주기
5. Update() - 매 프레임
```

## ⚠️ 문제 상황들

### **1. 의존성이 준비되지 않은 상태**
```csharp
public class CurrencyManager : MonoBehaviour
{
    [Inject] private IDataManager _dataManager;
    
    void Awake()
    {
        // ❌ 위험! _dataManager가 아직 주입되지 않았을 수 있음
        _dataManager.OnLoaded += UpdateCurrencies; // NullReferenceException!
    }
    
    void Start()
    {
        // ❌ 여전히 위험! 다른 매니저들이 초기화되지 않았을 수 있음
        _dataManager.Load();
    }
}
```

### **2. 초기화 순서 문제**
```csharp
// DataManager.cs
void Awake()
{
    Load(); // 데이터 로드
}

// CurrencyManager.cs  
void Awake()
{
    // ❌ DataManager.Awake()가 먼저 실행될지 보장할 수 없음
    var gold = Managers.Instance.Data.Current.gold; // 데이터가 아직 로드되지 않았을 수 있음
}
```

## ✅ Zenject 해결책

### **안전한 초기화**
```csharp
public class CurrencyManager : MonoBehaviour, IInitializable
{
    [Inject] private IDataManager _dataManager;
    
    void Awake()
    {
        // Unity 관련 초기화만 (UI 설정, 컴포넌트 참조 등)
        Debug.Log("CurrencyManager Awake");
    }
    
    public void Initialize()
    {
        // ✅ 안전! 모든 의존성이 주입되고 다른 매니저들도 준비됨
        _dataManager.OnLoaded += UpdateCurrencies;
        UpdateCurrencies(_dataManager.Current);
    }
    
    void Start()
    {
        // Unity 관련 시작 로직 (애니메이션, 물리 등)
    }
}
```

## 🎯 언제 무엇을 사용할까?

### **Awake() 사용 시기**
```csharp
void Awake()
{
    // ✅ Unity 컴포넌트 참조
    _rigidbody = GetComponent<Rigidbody>();
    _animator = GetComponent<Animator>();
    
    // ✅ 로컬 변수 초기화
    _health = 100f;
    _isAlive = true;
    
    // ✅ 자식 오브젝트 설정
    transform.Find("UI").gameObject.SetActive(false);
}
```

### **IInitializable.Initialize() 사용 시기**
```csharp
public void Initialize()
{
    // ✅ 다른 매니저와의 연결
    _dataManager.OnLoaded += HandleDataLoaded;
    _soundManager.PlayBGM("main_theme");
    
    // ✅ 비즈니스 로직 초기화
    LoadPlayerData();
    SetupEventHandlers();
    
    // ✅ 의존성이 필요한 작업
    var gold = _currencyManager.GetGold();
    UpdateUI(gold);
}
```

### **Start() 사용 시기**
```csharp
void Start()
{
    // ✅ 게임 시작 로직
    StartCoroutine(SpawnEnemies());
    
    // ✅ 애니메이션 시작
    _animator.SetTrigger("StartGame");
    
    // ✅ 물리 활성화
    _rigidbody.isKinematic = false;
}
```

## 📊 실제 사용 예시

```csharp
public class GameManager : MonoBehaviour, IInitializable
{
    [Inject] private IDataManager _dataManager;
    [Inject] private ISoundManager _soundManager;
    
    private PlayerController _player;
    private Camera _camera;
    
    void Awake()
    {
        // Unity 컴포넌트 설정
        _player = FindObjectOfType<PlayerController>();
        _camera = Camera.main;
        
        // 로컬 상태 초기화
        Time.timeScale = 1f;
    }
    
    public void Initialize()
    {
        // 매니저 간 연결
        _dataManager.OnSaved += HandleDataSaved;
        _soundManager.PlayBGM("game_music");
        
        // 게임 데이터 기반 초기화
        LoadGameState();
    }
    
    void Start()
    {
        // 게임 시작
        StartCoroutine(GameLoop());
        _player.EnableInput();
    }
}
```

## 🎯 결론

- **Awake()**: Unity 컴포넌트, 로컬 상태 초기화
- **IInitializable.Initialize()**: 의존성 기반 비즈니스 로직 초기화  
- **Start()**: 게임 시작, 애니메이션, 코루틴 등

**Zenject를 사용할 때는 의존성이 관련된 모든 초기화를 `Initialize()`에서 하는 것이 안전합니다!** 🚀

Ran tool

Zenject에서 **Installer**는 “어떤 인터페이스를 어떤 구현체에 바인딩할지”를 선언하는 클래스입니다. 크게 세 종류가 있어요:

1. **Project Installer**  
   - 전역 바인딩(앱 전체에서 한 번만 실행)  
   - `MonoInstaller` 또는 `ScriptableObjectInstaller`를 상속받아 `ProjectContext`(프리팹)나 `ProjectContext`를 통해 로드  
   - 예) `ProjectInstaller : MonoInstaller` (여기서 IDataManager, IUIManager, ISoundManager 등 전역 매니저를 바인딩)

2. **Scene Installer**  
   - 씬별로 필요한 바인딩을 추가로 선언  
   - `SceneContext` 오브젝트의 **Mono Installers** 또는 **Scriptable Object Installers** 리스트에 드래그·할당  
   - 예) `StageInstaller : MonoInstaller`를 만들어 Stage 고유 서비스나 컴포넌트를 바인딩

3. **Prefab Installer**  
   - 특정 프리팹이 로드될 때마다 함께 실행되어, 그 프리팹 내부 바인딩을 담당  
   - `FromComponentInNewPrefabResource` 등을 통해 씬에 자동 생성되는 SoundManager 같은 프리팹에 붙어 동작

———

### 씬 전용 Installer가 있는지 확인하는 방법

1. **코드 검색**  
   - 프로젝트 전반에서 `: MonoInstaller` 를 grep/검색해 보세요.  
   - (결과) `Assets/Scripts/Installers/ProjectInstaller.cs` 외에 사용자 정의 MonoInstaller가 없다면, 아직 씬 전용 Installer는 없는 겁니다.

2. **Installers 폴더 확인**  
   - `Assets/Scripts/Installers` 폴더에 `ProjectInstaller.cs` 외에 `StageInstaller.cs`, `LobbyInstaller.cs` 같은 파일이 있는지 확인  
   - 없으면 “씬 전용”으로 바인딩할 추가 항목은 없습니다.

3. **SceneContext Inspector**  
   - 각 씬에 배치된 `SceneContext`를 선택하고  
     - **Scriptable Object Installers**  
     - **Mono Installers**  
     - **Prefab Installers**  
     리스트가 비어있으면, “아직 씬 고유 Installer가 없다”는 뜻입니다.

———

### 필요하다면 씬 전용 Installer 만들기

1. `Assets/Scripts/Installers/StageInstaller.cs` (예시) 생성  
2. `public class StageInstaller : MonoInstaller` 상속  
3. `public override void InstallBindings()` 에 Scene 전용 바인딩 추가  
4. 각 Stage 씬의 `SceneContext` → **Mono Installers**에 `StageInstaller` 드래그  

이제 그 씬이 로드될 때마다 `ProjectInstaller`가 설치한 전역 바인딩에 더해 `StageInstaller`가 추가 바인딩을 수행합니다.


---
각 씬에서 Zenject이 동작하는 플로우는 이렇습니다:
1) ProjectContext(prefab) 의 InstallBindings 실행 → 전역 서비스 등록
2) 씬 로드 → SceneContext 찾음 → SceneInstaller.InstallBindings 실행 → 씬 전용 서비스 등록
3) DI 주입 단계 → SceneContext 하위(또는 GameObjectContext 하위)의 모든 [Inject] 필드·생성자에 주입
4) MonoBehaviour Awake → Start 순서로 동작
“각 씬에 SceneContext를 배치하고, 그 안에 반드시 해당 씬용 Installer를 등록해야”
그래야 [Inject] private ISceneManagerEx _sceneManager; 같은 필드가 정상적으로 채워집니다.

---

Play 모드에 들어가기 전, 에디터에서 다음과 같이 두 Context를 “미리” 설정해 두셔야 합니다.

1. ProjectContext (전역 바인딩)  
   • 경로  
     – Assets/Resources/Zenject/ProjectContext.prefab  
   • prefab 구성  
     – GameObject에 아래 두 컴포넌트가 붙어 있어야 합니다.  
       1) ProjectContext (스크립트)  
       2) ProjectInstaller (MonoInstaller)  
   • Inspector  
     – Scriptable Object Installers : (사용하는 SOInstaller이 있으면 추가)  
     – Mono Installers            : (빈 리스트여도 됨—prefab 자체에 ProjectInstaller가 붙어 있으므로 자동으로 실행)  
     – Prefab Installers          : (없어도 무관)  
     – Parent New Objects Under Context : 필요하면 체크  

   ※ Resources/Zenject 폴더에 있어야 Unity가 자동으로 로드합니다.

2. SceneContext (씬 단위 바인딩)  
   • 씬 열고 Hierarchy 최상단에 빈 GameObject 생성 → 이름을 “SceneContext”  
   • Add Component → SceneContext (스크립트)  
   • 씬별 Installer 등록  
     1) 다시 빈 GameObject 생성 → 이름 “SceneInstaller” (또는 “SelectCharacterSceneInstaller” 등)  
     2) 여기에 내가 만든 MonoInstaller(예: SelectCharacterSceneInstaller) 컴포넌트 추가  
     3) SceneContext Inspector → Mono Installers 섹션의 “+” 클릭 → 방금 만들었던 SceneInstaller GameObject 드래그 앤 드롭  
   • Inspector 옵션  
     – Auto Run: ✅  
     – Parent New Objects Under Context: 씬 오브젝트를 SceneContext 하위로 묶고 싶으면 ✅  

3. 빌드·리소스 확인  
   • ProjectContext.prefab이 반드시 **Assets/Resources/Zenject** 폴더에 있어야 자동 로드됩니다.  
   • Build Settings에 해당 씬이 포함되어 있는지 확인하세요.

――――――――――  
이렇게 설정해 두면,

1) 플레이 시작 전 ProjectContext가 로드되면서 ProjectInstaller.InstallBindings() 실행 → 전역 매니저들 바인딩  
2) 씬 로드 후 SceneContext가 “SceneInstaller.InstallBindings()” 실행 → 씬 전용 바인딩  
3) SceneContext 하위의 모든 MonoBehaviour에 `[Inject]` 필드 주입

…이 흐름이 보장되어, `CharacterSelector` 에서 `ISceneManagerEx _sceneManager`가 제대로 Resolve 됩니다.

1) 씬 개수 = Installer 스크립트 개수
2) 버튼·팝업 개수와는 무관
3) 팝업(prefab)에서 DI가 필요하면 Prefab Installers 또는 GameObjectContext를 사용하면 되고, 별도의 “SceneInstaller”는 필요 없습니다.

Main 씬 바인딩 → MainSceneInstaller.cs
Lobby 씬 바인딩 → LobbySceneInstaller.cs
SelectCharacter 씬 바인딩 → SelectCharacterSceneInstaller.cs
…와 같이, 씬이 