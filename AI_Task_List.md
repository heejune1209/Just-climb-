# 향후 AI에 요청할 Task 리스트

아래는 3대 향후 개선 계획(**랭킹 시스템**, **델타 기반 동기화**, **캐릭터 선택 개선**)별로 필요한 구성 요소 목록과 AI에게 요청할 수 있는 Task 리스트입니다. 
원하는 섹션/Task를 선택하셔서 "이 작업을 어떻게 해줘"라고 요청하시면 자세한 코드, 스크립트, 설정 예시를 제공해드립니다.

---

## 1. 랭킹 시스템 개선

### 1-1. 필요한 구성 요소

#### 클라이언트
| 파일/스크립트명               | 위치                                   | 역할 요약                                               |
|------------------------------|---------------------------------------|--------------------------------------------------------|
| **DeltaEvent.cs**            | `Scripts/Data/DeltaEvent.cs`          | 델타 모델({ key, value, timestamp })                    |
| **DataManager.cs**           | `Scripts/Managers/DataManager.cs`     | 델타 이벤트(OnDeltaGenerated) 생성                      |
| **RankingManager.cs**        | `Scripts/Managers/RankingManager.cs`  | 스테이지별 클리어타임 수집 → 로컬 랭킹 계산 → 델타 생성   |
| **UI_Leaderboard.cs**        | `Scripts/UI/UI_Leaderboard.cs`        | 로컬 랭킹 구독 → 글로벌 Top N 표시                      |

#### 서버
| 파일명                       | 경로                                           | 역할 요약                                              |
|-----------------------------|-----------------------------------------------|-------------------------------------------------------|
| **RankingController.cs**    | `Server/Controllers/RankingController.cs`     | 랭킹 기록 UPSERT, Top N 조회                          |
| **IUserRankingService.cs**  | `Server/Services/IUserRankingService.cs`      | 랭킹 저장·조회 인터페이스 정의                        |
| **UserRankingService.cs**   | `Server/Services/UserRankingService.cs`       | Redis Sorted Set 동기화 포함 UPSERT 로직               |
| **ConflictResolver.cs**     | `Server/Utils/ConflictResolver.cs`            | 랭킹 충돌 해결 로직                                   |

#### DB & 캐시
| 파일/스크립트                             | 위치                                    | 역할 요약                              |
|-------------------------------------------|----------------------------------------|---------------------------------------|
| **Migration_CreateRankingsTable.sql**     | `Server/Database/Migrations/`          | `rankings` 테이블 생성                   |
| **RedisSyncConfig.json**                 | `Server/Config/RedisSyncConfig.json`   | 랭킹 캐시 TTL, 인덱스 설정              |

### 1-2. 요청할 Task 리스트
1. **`RankingManager` 책임 분리 및 DI 도입**
2. **글로벌 랭킹 API 테스트 (xUnit) 작성**
3. **Redis 캐시 추상화 & 예외 처리 미들웨어 추가**
4. **EF Core Code-First 마이그레이션 예시**

---

## 2. 델타 기반 동기화 개선

### 2-1. 필요한 구성 요소

#### 클라이언트
| 파일/스크립트명                  | 위치                                   | 역할 요약                                      |
|---------------------------------|---------------------------------------|-----------------------------------------------|
| **DeltaEvent.cs**               | `Scripts/Data/DeltaEvent.cs`          | 델타 모델                                      |
| **DataManager.cs**              | `Scripts/Managers/DataManager.cs`     | 델타 이벤트 생성                                |
| **DataSyncManager.cs**          | `Scripts/Data/DataSyncManager.cs`     | 델타 큐·전송·재시도 로직                        |
| **OfflineCacheManager.cs**      | `Scripts/Data/OfflineCacheManager.cs` | 네트워크 상태 감지 → 싱크 제어                  |
| **UI_SyncStatus.cs**            | `Scripts/UI/UI_SyncStatus.cs`         | 싱크 상태 UI 표시                               |

#### 서버
| 파일명                          | 경로                                        | 역할 요약                                       |
|---------------------------------|--------------------------------------------|------------------------------------------------|
| **SaveController.cs**           | `Server/Controllers/SaveController.cs`     | 델타 수신 → UserStateService 위임               |
| **IUserStateService.cs**        | `Server/Services/IUserStateService.cs`     | 델타 병합·UPSERT 로직                           |
| **UserStateService.cs**         | `Server/Services/UserStateService.cs`      | DB 트랜잭션 및 ConflictResolver 호출 포함        |
| **ConflictResolver.cs**         | `Server/Utils/ConflictResolver.cs`         | 델타 충돌 해결 로직                              |

#### DB & 캐시
| 파일/스크립트                              | 위치                                         | 역할 요약                                |
|--------------------------------------------|---------------------------------------------|-----------------------------------------|
| **Migration_CreateUsersTable.sql**         | `Server/Database/Migrations/`               | `users` 테이블 생성                     |
| **Migration_CreateUserItemsTable.sql**     | `Server/Database/Migrations/`               | `user_items` UPSERT 스크립트             |
| **RedisSyncConfig.json**                  | `Server/Config/RedisSyncConfig.json`        | 델타 캐시 설정                          |
| **UpsertUserItem.sql**                    | `Server/Database/Scripts/UpsertUserItem.sql`| UPSERT 템플릿                           |

### 2-2. 요청할 Task 리스트
2. **EventBus 패턴 통합 예시**
3. **`DataSyncManager` 인터페이스 & Mock 테스트**
4. **`OfflineCacheManager` 리팩토링**
5. **Delta API Controller-Service-Repository 분리**

---

## 3. 캐릭터 선택 개선

### 3-1. 필요한 구성 요소

#### 클라이언트
| 파일/스크립트명              | 위치                                   | 역할 요약                         |
|-----------------------------|---------------------------------------|----------------------------------|
| **CharacterData.cs**        | `Scripts/Items/CharacterData.cs`      | 캐릭터 메타 SO                   |
| **CharacterDatabase.cs**    | `Scripts/Items/CharacterDatabase.cs`  | CharacterData 로드 API           |
| **UI_SelectCharacter.cs**   | `Scripts/UI/UI_SelectCharacter.cs`    | 캐릭터 선택 UI 및 델타 이벤트 발행|

#### 서버
| 파일명                        | 경로                                        | 역할 요약                                 |
|------------------------------|--------------------------------------------|-------------------------------------------|
| **CharacterController.cs**   | `Server/Controllers/CharacterController.cs`| 캐릭터 선택 델타 수신 및 저장             |
| **IUserStateService.cs**     | `Server/Services/IUserStateService.cs`     | Character 저장 로직 포함                  |
| **UserStateService.cs**      | `Server/Services/UserStateService.cs`      | DB 트랜잭션 및 ConflictResolver 호출      |

#### DB & 캐시
| 파일/스크립트                                    | 위치                                  | 역할 요약                          |
|-------------------------------------------------|--------------------------------------|-----------------------------------|
| **Migration_CreateCharacterHistoryTable.sql**   | `Server/Database/Migrations/`        | `user_character_history` 테이블 생성|

### 3-2. 요청할 Task 리스트
1. **`CharacterDatabase` API 개선 & DI**
2. **UI MVVM 구조로 캐릭터 선택 리팩토링**
3. **캐릭터 델타 Controller-Service-Repository 분리**
4. **CharacterHistory EF Core 마이그레이션**


-----------------------------------------------


---
### 4-1. 필요한 구성 요소

#### 클라이언트
| 파일/스크립트명       | 위치                                          | 역할 요약                                        |
|----------------------|-----------------------------------------------|--------------------------------------------------|
| **ItemDatabase.cs**  | `Assets/Scripts/Data/StaticData/ItemDatabase.cs` | ScriptableObject 로드 및 아이템 정의 딕셔너리 초기화 |
| **ItemData.asset**   | `Assets/Resources/ScriptableObjects/Items`       | 아이템 정의 ScriptableObject                       |
| **ItemManager.cs**   | `Assets/Scripts/Managers/ItemManager.cs`          | IItemUse 매핑 및 아이템 사용/쿨다운 로직          |

### 4-2. 요청할 Task 리스트
1. **ItemDatabase에 `IInitializable` 구현 및 `Initialize()` 메서드로 `Init()` 호출하도록 변경**
2. **ProjectInstaller에 `ItemDatabase` `IInitializable` 바인딩 추가(`.AsSingle().NonLazy()`)**
3. **`ItemDatabase.Init()` 호출 순서 검증 및 예외 처리 추가**
4. **`ItemManager.Init()` 전에 `ItemDatabase`가 초기화되도록 순서 보장**
5. **ScriptableObject `ItemData` 로드 경로(`ScriptableObjects/Items`) 일치 여부 확인**

## 4. 전반적인 아키텍처 문제점 및 개선 가이드

현재 아키텍처(위 클래스 다이어그램 기반)에서 눈에 띄는 문제점과 개선 아이디어를 정리하면 다음과 같습니다.

1. 전역 싱글톤 "Managers" 컨테이너  
  • 하나의 클래스에 모든 매니저가 집결 → 결합도 급상승, 테스트·유지보수 어려움  
  → DI 컨테이너(Zenject, UniRx DI)나 서비스 로케이터로 대체해 책임 분리

2. 매니저별 과중한 책임  
  • GameManager가 타이머, 데스 카운트, 깃발 위치, 플레이어 관리 all 를 담당 → SRP 위반  
  → TimerManager, DeathManager, FlagManager 등으로 쪼개고, GameManager는 orchestrator 역할만 수행

3. 이벤트 구독/발행 분산  
  • 각 매니저·컨트롤러가 C# 이벤트 직접 들고 있음 → 해제 누락 시 메모리 누수·디버깅 난이도 상승  
  → 중앙 EventBus(MessageBroker) 패턴 도입으로 구독·발행 로직 통합

4. PlayerController 상속 구조  
  • CS/TPS 컨트롤러에 중복 속성(hideCursor, clamp 등) → 유지보수 비용 증가  
  → 입력(Input)·이동(Mover)·카메라(Camera) 컴포넌트로 분리하고, 전략패턴·컴포지션 활용

5. AbstractAbility 기반 능력 시스템  
  • 추상 클래스 상속에 의존 → 능력 추가 시 클래스 폭발 위험  
  → ScriptableObject+State Pattern or ECS 형태로 전환

6. 리소스·풀 관리 매니저  
  • ResourceManager, PoolManager가 직접 로드·풀링 로직 구현 → Unity Addressables·AssetBundle 활용 권장  
  → 비동기 로딩, 메모리 관리 최적화

7. 씬 전환 로직의 중복  
  • SceneManagerEx로 씬별 로딩·전환 제어가散逸 → 상태 머신(StateMachine)으로 통합

8. 테스트 환경 부재  
  • static, 싱글톤, Unity API 직접 호출이 많아 단위 테스트 불가  
  → 인터페이스 추상화 후 Mock 주입, Editor 로직 분리

9. 데이터 직렬화 한계  
  • JsonUtility 사용 → `List<T>`, `Dictionary<K,V>` 등 컬렉션 직렬화 번거로움  
  → Newtonsoft.Json 혹은 MessagePack 등 대체 포맷 도입

---

### 4-2. GameManager SRP 관리 가이드 
(Task 예시)네, 지금 프로젝트 규모가 작고 GameManager가 감당하는 로직이 많지 않다면 당장 SRP 완벽하게 준수하기 위해 분할하지 않아도 무방합니다. 다만 다음 사항을 고려해 보시면 좋아요:

1. 기술 부채(Technical Debt) vs. YAGNI  
   - **YAGNI(You Aren't Gonna Need It)** 관점에서 "지금 당장 필요 없으면 미뤄도 된다"는 건 합리적입니다.  
   - 다만, 나중에 분할이 어려워지지 않도록 최소한의 경계(예: 메서드나 지역 주석)라도 미리 표시해 두면 좋습니다.

2. 언제 리팩토링할지 기준 세우기  
   - 메서드가 50줄 넘어가거나, 의존성이 5개 이상 늘어날 때  
   - 단위 테스트가 어려워질 때  
   - 새로운 기능 추가 시 GameManager가 자주 수정될 때  
   이런 시점을 "리팩토링 트리거"로 정해 두면 감각적으로 놓치지 않고 대응할 수 있습니다.

3. 완전 분할 전의 가벼운 대안  
   - **타이머 로직**만 `TimerService` 같은 내부 클래스로 감싸두기  
   - **이벤트 핸들러**(OnPlayerDead, OnTimerUpdated)만 별도 `GameEvents` 클래스로 이동  
   - 이런 식으로 조금씩 "의존성 경계"를 만들어 두면, 실제 분할이 필요해졌을 때 작업량이 크게 줄어듭니다.

4. 장기적인 관점  
   - 분할 작업은 한 번에 크게 하기보다는, 기능 추가나 수정이 있을 때마다 **작은 스텝**으로 나누어 진행하는 게 부담이 적습니다.  
   - 테스트 커버리지가 확보되어 있으면 대규모 리팩토링도 안전하게 진행할 수 있습니다.

결론적으로, 지금 당장은 미루셔도 괜찮지만  
"언제", "어떤 기준"으로 분할할지 미리 계획해 두면  
나중에 GameManager가 과부하로 변질되는 걸 방지할 수 있습니다.  
필요하다면 간단한 기준 설정이나, TimerService 캡슐화 예시 같은 **작은 첫걸음**부터 시작해 보시길 추천드립니다.


네, 맞습니다. DI(Dependency Injection)를 도입하면 지금의 싱글톤·전역 의존성 구조보다 훨씬 유연하고 유지보수하기 쉬워집니다.

주요 장점  
1. 결합도 감소  
   • 직접 `Managers.Instance` 를 참조하던 곳을 생성자 주입으로 바꿔, 클래스 간 의존성을 명확히 분리  
2. 단위 테스트 용이  
   • 인터페이스를 주입받으면 Mock/Stub을 끼워 넣어 단위 테스트 작성 가능  
3. 구현 교체·확장 유연성  
   • 예를 들어 `IDataSyncService` 인터페이스만 정의해두면 실제 HTTP 구현, 로컬 목 구현 등을 쉽게 교체  
4. 생명주기·스코프 관리  
   • 싱글톤, 요청 단위, 트랜잭션 단위 등 객체 수명 주기를 컨테이너가 책임져 줌  

물론 초기 설정(컨테이너 구성, 바인딩 코드 작성)과 코드 수정이 필요하지만,  
– 장기적으로 새로운 기능 추가, 리팩토링, 테스트 커버리지 확대 시 드는 비용이 훨씬 줄어듭니다.  

작업 흐름 예시  
1. **DI 컨테이너 설치**  
   – Zenject, UniRx DI, VContainer 등 Unity 친화적 라이브러리 선택  
2. **인터페이스 추출**  
   – `DataManager` → `IDataManager`  
   – `DataSyncManager` → `IDataSyncService` 등  
3. **바인딩 설정**  
   – 프로젝트 시작 시점(예: `ProjectContext`나 `Bootstrapper`)에 구현체 등록  
4. **의존 객체 주입**  
   – 생성자 인자 혹은 `[Inject]` 어트리뷰트로 필요한 서비스 주입  
5. **테스트 코드 작성**  
   – Mock/Stub을 DI 컨테이너에 바인딩해 단위 테스트 작성  

이렇게 단계별로 진행하면, 현재 아키텍처보다 훨씬 견고하고 확장성 높은 코드베이스를 만들 수 있습니다.

---

## 5. Zenject를 이용한 매니저 아키텍처 리팩토링

### 5-1. 수정할 스크립트 명단

- Assets/Scripts/Managers/Managers.cs
- Assets/Scripts/Managers/DataManager.cs
- Assets/Scripts/Managers/CurrencyManager.cs
- Assets/Scripts/Managers/ItemManager.cs
- Assets/Scripts/Managers/StageManager.cs
- Assets/Scripts/Managers/GameManager.cs
- Assets/Scripts/Managers/ResourceManager.cs
- Assets/Scripts/Managers/UIManager.cs
- Assets/Scripts/Managers/SceneManagerEx.cs
- Assets/Scripts/Managers/SoundManager.cs
- Assets/Scripts/Managers/PoolManager.cs
- Assets/Scripts/Managers/ItemDatabase.cs
- Assets/Scripts/Managers/RankingManager.cs

### 5-2. 리팩토링 Task

1. 각 매니저 클래스에 대응하는 인터페이스 정의 및 분리 (예: IDataManager, ICurrencyManager 등)
2. `Managers.cs`를 Zenject Installer 클래스로 변환하여 바인딩 로직 구현
3. 각 매니저 클래스 생성자에 `[Inject]` 어트리뷰트 추가 및 생성자 주입 방식 적용
4. 프로젝트 초기화 시점에 `ProjectContext` 또는 별도 Installer에서 매니저 구현체를 바인딩 등록
5. 기존 `Managers.Instance` 참조를 제거하고, 필요한 클래스에 인터페이스를 주입받도록 코드 수정

### 5-3. ProjectContext 설정 및 Installer 연결

- **Installer 스크립트 작성**
  - `Assets/Scripts/Installers/ProjectInstaller.cs` 파일을 만들고, `MonoInstaller`를 상속하여 `InstallBindings()`에서 매니저 바인딩 로직을 구현합니다.
  ```csharp
  using Zenject;
  public class ProjectInstaller : MonoInstaller
  {
      public override void InstallBindings()
      {
          Container.Bind<IDataManager>().To<DataManager>().AsSingle();
          Container.Bind<ISoundManager>().To<SoundManager>().AsSingle();
          // 기타 매니저 바인딩
      }
  }
  ```

- **ProjectContext 프리팹에 등록**
  1. `Assets/Resources/Zenject/ProjectContext.prefab`을 선택합니다.
  2. Inspector에서 **Mono Installers** 리스트 옆 `+` 버튼을 클릭합니다.
  3. 드롭다운에서 `ProjectInstaller`를 선택하거나 드래그 앤 드롭으로 추가합니다.

- **검증**
  1. 에디터에서 Play 모드를 실행합니다.
  2. Console에 Zenject 바인딩 로그가 출력되는지 확인합니다.
  3. `[Inject]` 어트리뷰트를 사용해 주입된 매니저가 동작하는지 테스트합니다.

  맞습니다. Zenject은 일반 C# 클래스에서는 **생성자(Constructor) 주입**을 그대로 지원하고, MonoBehaviour 컴포넌트에는 **메서드 주입(Method Injection)** 을 권장합니다.

---

## 1. 일반 클래스에서 생성자 주입

```csharp
public interface IDataSyncService
{
    void Sync();
}

public class DataSyncManager : IDataSyncService
{
    readonly IDataManager _dataManager;
    readonly IOfflineCacheManager _cacheManager;

    // 생성자에 [Inject]를 붙이지 않아도 됩니다.
    // MonoBehaviour가 아닌 순수 C# 클래스는 컨테이너가 생성자를 찾아서 주입해 줍니다.
    public DataSyncManager(IDataManager dataManager, IOfflineCacheManager cacheManager)
    {
        _dataManager   = dataManager;
        _cacheManager  = cacheManager;
    }

    public void Sync() { /* … */ }
}
```

```csharp
// Installer 바인딩 예시
public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<IDataManager>().To<DataManager>().AsSingle();
        Container.Bind<IOfflineCacheManager>().To<OfflineCacheManager>().AsSingle();
        Container.Bind<IDataSyncService>().To<DataSyncManager>().AsSingle();
    }
}
```

이렇게 하면 `new DataSyncManager(...)` 대신  
```csharp
[Inject] IDataSyncService _syncService;
```
로 바로 주입받아 사용할 수 있습니다.

---

## 2. MonoBehaviour에서는 메서드 주입

Unity의 MonoBehaviour는 생성자를 직접 쓸 수 없어, Zenject 에서는 별도의 **Construct** 메서드 주입 방식을 씁니다.

```csharp
public class PlayerController : MonoBehaviour
{
    IInputService _inputService;

    // [Inject]가 붙은 메서드는 Awake 직후에 Zenject가 호출해 줍니다.
    [Inject]
    public void Construct(IInputService inputService)
    {
        _inputService = inputService;
    }

    void Update()
    {
        _inputService.HandleInput();
    }
}
```

```csharp
// Installer 바인딩 예시
public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<IInputService>().To<InputService>().AsSingle();
        // ... 기타 바인딩
    }
}
```

이렇게 하면 `PlayerController`를 씬에 배치만 해두면, `Construct` 메서드로 자동 주입되어 바로 사용 가능합니다.

---

### 요약

- **순수 C# 클래스** → 생성자 주입(Constructor Injection)  
- **MonoBehaviour 클래스** → `[Inject]` 메서드 주입(Method Injection)  
- **Installer** 에서 `Container.Bind<인터페이스>().To<구현>().AsSingle()` 방식으로 바인딩  

이 두 가지 방식을 상황에 맞게 사용하시면 됩니다!

지금처럼 스테이지, 아이템, 장애물 정도만 "컨텐츠 확장"이 필요하다면, 굳이 서비스·오케스트레이터·이벤트 버스까지 복잡하게 나누지 않아도 됩니다. 대신
매니저 인터페이스 + Zenject DI 만 유지
컨텐츠는 ScriptableObject 로 관리
MonoBehaviour 에서는 [Inject] IStageManager _stage; 처럼 필요한 매니저만 받아서 사용
이 정도만 적용해도 책임 분리는 충분합니다.

---
프로젝트에서 앞으로 "캐릭터 종류, 업적, 아이템, 장애물, 스테이지" 같은 **컨텐츠 데이터**만 계속 추가된다면, Zenject 같은 전통적인 DI 컨테이너를 도입하는 것은 다음과 같은 장단점이 있습니다.

장점  
1. **의존성 명시성**  
   – 필요한 서비스(매니저)를 `[Inject]`로 선언만 해 두면, Inspector-연결 없이도 항상 올바른 인스턴스를 쓸 수 있습니다.  
2. **테스트 용이성**  
   – 인터페이스 기반 설계 덕분에, 나중에 단위 테스트나 에디터 스크립트에서 Mock/Stub을 주입하기 쉽습니다.  
3. **유연한 확장**  
   – AOP(로깅/트랜잭션)나 Signal(이벤트 버스) 같은 Zenject 고급 기능을 적용하기 편리합니다.

단점  
1. **초기 학습 비용**  
   – Installer, Context 프리팹 세팅, 바인딩 룰 숙지가 필요합니다.  
2. **코드량 증가**  
   – 모든 서비스마다 인터페이스와 Installer 설정을 추가로 작성해야 합니다.  
3. **실제 변화가 적을 때 과잉 설계 위험**  
   – 단순히 "데이터 추가"만 계속된다면, 굳이 런타임 컨테이너를 복잡하게 운영할 필요가 없을 수 있습니다.

결론적으로—  
- **"컨텐츠만 늘어나는 구조"**라면  
  → **ScriptableObject + SO기반 데이터 매니저/조회 API** 형태로만 구현해도 충분히 깔끔합니다.  
  → 예: `CharacterDatabase : ScriptableObject` + `ICharacterManager.Get(id)`  
  → 몽땅 SO 에서만 데이터 추가 · Inspector 에서만 에셋 연결 → 코드 수정 없이 컨텐츠 확장 가능  

- **"서비스 간 복잡한 의존관계"**나 **테스트, 런타임 플러그인 교체, AOP** 등이 필요하다면  
  → Zenject DI 도입이 유리합니다.  

따라서  
• 만약 "단순히 새 캐릭터·아이템·스테이지 SO 에셋을 추가" 하는 선에서 끝난다면, Zenject는 **약간 과한 설계**가 될 수 있습니다.  
• 반면에 "매니저 로직의 확장, 유닛테스트, 런타임 구성 변경" 등을 고려한다면, 지금부터 DI 컨테이너를 적용해 두는 편이 장기적으로 득이 됩니다.  

프로젝트의 **현재 요구사항**이 콘텐츠 추가에만 머문다면, **ScriptableObject + 간단한 IManager/SO 패턴**을 유지하시고, 필요할 때만 Zenject Installer를 도입해 보세요.
---

아니요. 만약 "새로운 캐릭터·아이템·장애물·스테이지" 같은 **콘텐츠 데이터**만 계속 추가되는 게 전부라면,  

  • **ScriptableObject + Service Locator 패턴(Managers.Instance…)**  
    – 에셋(스크립터블 오브젝트)만 추가하면 되고  
    – 매니저들은 하나의 싱글톤 컨테이너에서 일괄 관리하니 구현·설정이 매우 간단  
    – 러닝타임 DI 설정(Installer, Context 프리팹 등)이 필요 없어서 진입장벽이 낮음  

이렇게 **가벼운 구조**가 오히려 더 효율적일 수 있습니다.  

반면, 아래와 같은 요구가 생긴다면 **Zenject 기반 DI**가 강력해집니다.  
- 매니저 간 의존관계 복잡도 증가  
- UI·게임 플로우마다 다른 구현체 교체  
- 단위 테스트(Mock/Stub) 작성  
- 런타임 설정 변경, SignalBus, AOP(로깅·트랜잭션) 등 고급 기능  

정리하면:  
1. **지금처럼 단순한 콘텐츠 추가가 주가 된다면**  
   → ScriptableObject + Service Locator 패턴으로 충분  
2. **테스트·유연성·확장성·런타임 구성 같은 추가 요구**가 생기면  
   → Zenject DI 패턴 도입이 합리적  

따라서 현재 상황에선 "SO + 싱글톤 컨테이너"로 시작하시고, 나중에 필요해질 때 Zenject DI를 도입해도 전혀 늦지 않습니다.


콘텐츠 데이터 추가만 할 때는 SO+Service Locator로 충분히 가볍고 빠릅니다.
서버 연동·Redis 캐싱 등 외부 의존성을 다루려면,
IApiClient, IRedisCacheService 같은 인터페이스 추상화
Zenject Installer를 통한 DI 바인딩
생성자/메서드 주입 방식
을 적용하는 편이 훨씬 안전하고 유연합니다.
즉, 단순한 콘텐츠 확장에 그친다면 SO+싱글톤이 OK,
서버·캐시 연동까지 고려한다면 DI 패턴 기반 설계로 전환하는 게 바람직합니다.

---

## 6. DI 리팩토링 완료 후 남은 작업 (2024년 현재)

### 6-1. 🔧 코드 리팩토링 필요 (AI 작업)

#### Obstacle 스크립트 DI 전환
- [ ] **PoolableObstacle.cs** - `Managers.Instance.Pool.Push()` → `IPoolManager` 주입
- [ ] **RockDropper.cs** - `Managers.Instance.Resource.Instantiate()` → `IResourceManager` 주입  
- [ ] **RollingSpawner.cs** - `Managers.Instance.Resource.Instantiate()` → `IResourceManager` 주입
- [ ] **CannonShooter.cs** - `Managers.Instance.Resource.Instantiate()` → `IResourceManager` 주입
- [ ] **Cannon.cs** - `Managers.Instance.Sound.PlaySFX()` → `ISoundManager` 주입
- [ ] **RockGenerator.cs** - `Managers.Instance.Sound.PlaySFX()` → `ISoundManager` 주입  
- [ ] **KnockBack.cs** - `Managers.Instance.Sound.PlaySFX()` → `ISoundManager` 주입
- [ ] **Jumping.cs** - `Managers.Instance.Sound.PlaySFX()` → `ISoundManager` 주입

#### 정리 작업
- [ ] **Managers.cs 완전 제거** - 위 Obstacle 스크립트 DI 전환 완료 후
- [ ] **DataManagerTester.cs** - 주석 처리된 `Managers.Instance` 코드 정리 또는 DI 테스트 코드로 전환

### 6-2. 🎮 Unity 에디터 바인딩 작업 (수동 작업)

#### ProjectContext 설정 확인
- [ ] **ProjectContext 프리팹 존재 확인**
  - 경로: `Assets/Resources/ProjectContext.prefab`
  - 없으면 생성: `GameObject > Zenject > Project Context`

- [ ] **ProjectInstaller 바인딩 확인**
  - ProjectContext 프리팹 선택
  - Inspector > Mono Installers 리스트에 `ProjectInstaller` 추가되어 있는지 확인
  - 없으면 `+` 버튼 클릭 후 `ProjectInstaller` 선택

#### Scene Context 설정 (필요시)
- [ ] **각 씬별 SceneContext 확인**
  - MainScene, LobbyScene, StageScene에 SceneContext가 필요한지 검토
  - 씬별 특화 매니저가 있다면 SceneInstaller 생성 및 바인딩

#### SoundManager 프리팹 바인딩 확인  
- [ ] **SoundManager 프리팹 경로 확인**
  - 현재 ProjectInstaller에서 `"Prefabs/SoundManager"` 경로로 바인딩
  - 실제 프리팹이 해당 경로에 존재하는지 확인
  - 경로가 다르면 ProjectInstaller.cs에서 수정 필요

### 6-3. 🧪 테스트 및 검증 작업

#### 런타임 테스트
- [ ] **Play 모드에서 DI 주입 확인**
  - Console에 Zenject 바인딩 로그 출력 확인
  - 각 매니저가 올바르게 주입되는지 확인
  - NullReferenceException 발생하지 않는지 확인

- [ ] **씬 전환 테스트**
  - Main → Lobby → Stage → Main 순서로 씬 전환
  - 각 씬에서 UI, 사운드, 게임 로직이 정상 동작하는지 확인

#### 기능별 테스트
- [ ] **UI 시스템** - 팝업 열기/닫기, 씬 UI 전환
- [ ] **사운드 시스템** - BGM, SFX 재생
- [ ] **데이터 시스템** - 저장/로드, 통화 관리
- [ ] **아이템 시스템** - 아이템 사용, 인벤토리
- [ ] **스테이지 시스템** - 스테이지 선택, 클리어 기록

### 6-4. 📋 선택적 개선 작업 (우선순위 낮음)

#### 아키텍처 개선
- [ ] **GameManager SRP 분리** (필요시)
  - TimerManager, DeathManager, FlagManager 등으로 분할
  - 현재 규모에서는 불필요할 수 있음

- [ ] **EventBus 패턴 도입** (필요시)  
  - 매니저 간 이벤트 통신을 중앙화
  - 현재는 직접 참조로도 충분함

#### 테스트 환경 구축
- [ ] **Unit Test 환경 설정**
  - NUnit, Zenject Test Framework 설정
  - Mock 객체를 이용한 매니저별 단위 테스트 작성

### 6-5. 🚨 주의사항

#### DI 전환 시 고려사항
- **MonoBehaviour vs 일반 클래스**
  - MonoBehaviour: `[Inject] void Construct()` 메서드 주입
  - 일반 클래스: 생성자 주입

- **순환 참조 방지**
  - 매니저 간 상호 참조가 있는지 확인
  - 필요시 인터페이스 분리나 이벤트 기반 통신으로 해결

- **초기화 순서**
  - IInitializable 인터페이스 구현으로 초기화 순서 보장
  - Zenject가 의존성 그래프에 따라 자동 정렬

#### 에디터 작업 시 주의사항
- **ProjectContext는 씬에 배치하지 말 것**
  - Resources 폴더의 프리팹으로만 존재해야 함
  - 씬에 배치하면 중복 인스턴스 생성 위험

- **바인딩 누락 확인**
  - Play 모드에서 "No binding found" 에러 발생 시
  - ProjectInstaller에서 해당 인터페이스 바인딩 추가

---

## 7. 완료된 DI 리팩토링 현황 ✅

### 완전히 전환된 시스템
- ✅ **Manager 시스템** - 모든 매니저 인터페이스화 및 DI 주입
- ✅ **UI 시스템** - 계층적 UI 구조 유지하며 DI 전환  
- ✅ **Scene 관리** - BaseScene 및 각 씬별 DI 적용
- ✅ **Items 시스템** - ItemInput, FlagUse 등 DI 전환
- ✅ **Data 모델** - Models, StaticData 폴더 (변경 불필요)
- ✅ **Utils 클래스** - 정적 유틸리티 (변경 불필요)
- ✅ **Trigger 스크립트** - LobbyTrigger, ZoneTrigger DI 적용
- ✅ **DataSyncManager** - DI 및 IInitializable 적용

### 핵심 성과
1. **의존성 명확화** - 모든 의존성이 생성자/메서드 주입으로 명시
2. **테스트 준비** - 인터페이스 기반으로 Mock 주입 가능
3. **초기화 안전성** - Zenject 의존성 그래프로 순서 보장
4. **순환 참조 해결** - Optional 주입으로 순환 의존성 처리
5. **코드 일관성** - 전체 프로젝트에 통일된 DI 패턴 적용

---

*마지막 업데이트: 2024년 12월*

### 클라이밍 관련 스크립트 목록

- `Assets/Dias Games/Climbing System/Scripts/LobbyCharater.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/CSPlayerController.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Ledge.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/WallRunTrigger.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Ladder.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Components/CameraTargetFollow.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Components/ClimbIK.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Components/CastDebug.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Climb State Machine/ClimbStateBase.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Climb State Machine/ClimbStateContext.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Climb State Machine/ClimbIdleState.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Climb State Machine/ClimbJumpState.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Climb State Machine/ClimbShimmyState.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Climb State Machine/ClimbUpState.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Climb State Machine/ClimbDropState.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Climb State Machine/CornerInState.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Climb State Machine/CornerOutState.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Abilities/ClimbAbility.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Abilities/ClimbLadderAbility.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Abilities/DropToClimb.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Abilities/ShortClimb.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Abilities/Vault.cs`
- `Assets/Dias Games/Climbing System/Scripts/Mono Behaviour/Abilities/WallRun.cs`

### Selection Mode 기능 구현을 위한 스크립트 분류 및 수정 제안

#### 1) 스크립트 분류

- 필요 스크립트:
  - **Climbing System**
    - `ClimbAbility.cs`
    - 상태머신: `ClimbStateContext.cs`, `ClimbStateBase.cs`, `ClimbIdleState.cs`, `ClimbJumpState.cs`, `ClimbUpState.cs`, `ClimbShimmyState.cs`, `ClimbDropState.cs`, `CornerInState.cs`, `CornerOutState.cs`
    - 홀드 정의: `Ledge.cs`, `WallRunTrigger.cs`, `Ladder.cs`
    - 기타 연계 Abilities: `ClimbLadderAbility.cs`, `DropToClimb.cs`, `ShortClimb.cs`, `Vault.cs`
    - 시각컴포넌트: `CameraTargetFollow.cs`, `ClimbIK.cs`

  - **Third Person System**
    - `AbstractAbility.cs`, `AbilityScheduler.cs`
    - `TPSPlayerController.cs` (입력 라우팅)
    - 이동 컴포넌트: `Mover.cs` or `RigidbodyMover.cs`, `IMover.cs`

- 불필요 스크립트 (Selection Mode 구현에 직접 관여하지 않음):
  - Third Person System: `Combat/*`, `PushAbility.cs`, `AirControlAbility.cs`, `Roll.cs`, `Strafe.cs`, `Crouch.cs`, `Crawl.cs` 등 전투·이동 보조 기능
  - Interfaces: `IDamage.cs`, `IDraggable.cs`, `IHandIKTarget.cs` 등
  - 에디터 전용/유틸: `CharacterActions.cs`, `SetupProjectSettings.cs`, `ReadOnlyAttribute.cs`

#### 2) 수정 및 추가 제안

1. ClimbAbility에 **SelectionMode** 상태 추가
   - 기존 상태머신에 `SelectionMode` enum 삽입
   - W/A/S/D 입력 시 `EnterSelectionMode(direction)` 호출

2. **근처 홀드 탐색** 로직
   - `float detectionRadius` 파라미터 추가
   - `Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, buffer, holdLayerMask)` 사용
   - 호출 빈도 제한(예: 최소 0.1초)
   - 플레이어 상태(Idle/Moving)에 따라 `detectionRadius` 동적 조정

3. **방향별 후보 분류 & 하이라이트**
   - `List<Ledge> nearbyHolds`, `List<Ledge> candidateHolds` 관리
   - `Vector3 localPos = player.InverseTransformPoint(holdPos).normalized` 기준으로 Forward/Left/Down/Right 분류
   - 하이라이트용 `Material highlightMat`, `Material selectedMat` 필드 추가

4. **후보 순환 처리**
   - LeftArrow/RightArrow 입력 상태 머신에 통합
   - 이전/현재 인덱스 머티리얼 교체, `currentIdx = (idx ± 1 + count) % count`
   - W/A... 키 릴리즈 시 선택 취소 로직

5. **Fallback 로직**
   - `candidateHolds.Count == 0`일 때 기본 점프 or 드롭 기능 실행
   - 선택 모드에서 키 릴리즈 시 상태 복귀

6. **성능 최적화**
   - `Physics.OverlapSphereNonAlloc` + LayerMask 필터링
   - GC 방지를 위한 재사용 버퍼

7. **UI/시각적 피드백**
   - 선택 모드 진입 시 간단한 UI 텍스트 또는 아이콘 표시
   - 완료 시 카메라 Zoom 효과 또는 파티클 이펙트 추가