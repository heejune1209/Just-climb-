## Just Climb!
🗓️ 프로젝트 개요
- 기간 : 2023.09 ~ 2023.12
- 인원 : 4인 (기획 1, 아트1, 프로그래머 2)
- 역할 : 메인 프로그래머
- 도구 : Unity3D, C#, ASP.NET Core Web API, Entity Framework Core, Github
- 장르 : 어드벤처, 클라이밍, 3인칭 백뷰 
- 플랫폼 : PC

---
프로젝트 리펙토링으로 인해 설명 업데이트 예정

## 프로젝트 설명
- Unity 엔진 기반 3D 백뷰 클라이밍 게임
- 홀드를 이용한 암벽 등반과 장애물을 파훼하여 산 정상에 오르는 게임
- 총 8개 Stage 구성
- **실시간 랭킹 시스템**과 **온라인 데이터 동기화** 기능 포함
- **Zenject DI 기반 모듈화 아키텍처**로 확장성과 유지보수성 확보

## 설계서
### Game Flow
![Image](https://github.com/user-attachments/assets/679a1411-5d48-4aaa-879f-68a8efc2cd31)
- 씬 전환 기반 구조로 타이틀 → 로비 → 스테이지 → 결과로 이어지는 흐름
- 각 Scene은 UI 구조 및 매니저 관리 하에 독립적으로 동작.

### Game Structure
![image](https://github.com/user-attachments/assets/86640143-8964-4251-a8f2-a2927ace9c44)

![image](https://github.com/user-attachments/assets/3c195144-73de-40e6-8450-95cbd6cc1a0c)

## 주요 구성 요소

### 아키텍처 개선
- **Zenject DI 컨테이너** 도입으로 기존 Service Locator 패턴에서 **의존성 주입** 방식으로 전환
- **UI 계층화** → `UI_Base`→`UI_Scene`/`UI_Popup`
- **Scene 로직** → `BaseScene.Awake()` → `Init()` 가상 호출 → 자식 `MainScene/LobbyScene/StageScene.Init()` → `UIManager.ShowSceneUI<…>()`
- **4-Tier 아키텍처**를 통해 **Persistence → Domain → Infrastructure → UI** 명확한 책임 분리
- **ProjectInstaller**를 통한 **계층별 의존성 주입** 및 **자동 초기화** 관리
- **메모리 누수 방지**: 모든 매니저에 `IDisposable` 구현으로 이벤트 해제 및 리소스 정리

### Persistence Layer
- **[DataManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/DataManager.cs)**  
  - 로컬 JSON(`save.json`)의 읽기/쓰기 담당.  
   - `Init()` → 파일 복사/로드  
   - `Load()` → `OnLoaded` 이벤트  
   - `Save()` → `OnSaved` 이벤트  
   - `DeleteAllData()` → 데이터 초기화  
   - **델타 이벤트 시스템**: 데이터 변경 시 `OnDeltaGenerated` 이벤트 발생으로 실시간 동기화

- **[SaveData](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Data/Models/SaveData.cs)**  
  - 게임 상태를 직렬화하는 모델 클래스  
   - `gold`, `gems`, `selectedCharacter`  
   - `items`: `InventoryItem[]`  
   - `stageClears`, `stageRewards`, `stageTimes`, `stagePlayTimes`, `stageDeathCounts`  
   - `stageFlagPositions` 등의 데이터들을 직렬화
   - **최고 기록 추적**: `bestClearTimes`, `bestDeathCounts` 추가

- **[InventoryItem](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Data/Models/InventoryItem.cs)**  
  - 아이템별 `itemId`·`count`를 저장하는 클래스

### Domain Layer
- **[CurrencyManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/CurrencyManager.cs)**  
  - `DataManager` 이벤트 구독 → 골드/보석 관리  
   - `OnGoldChanged(int)`, `OnGemsChanged(int)`  
   - `GetGold()`, `AddGold()`, `SpendGold()`  

- **[ItemManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/ItemManager.cs)**  
  - ScriptableObject 기반 아이템 사용 로직 로드  
   - `LoadItemUses()` → `IItemUse` 구현체 자동 등록  
   - 수량·쿨타임·버프 관리, `UseItem()` API  

- **[StageManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/StageManager.cs)**  
  - 스테이지 클리어 플래그·보상·기록 전담  
   - `SetCleared(stage, gems, time, deaths)`  
   - `OnStageUnlocked`, `OnBestRewardUpdated`, `OnBestTimeUpdated`, `OnBestDeathUpdated`  

- **[RankingManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/RankingManager.cs)**  
  - **서버 기반 실시간 랭킹 시스템**
  - 클리어 타임 및 사망 횟수 기준 정렬 지원
  - 캐시 기반 성능 최적화
  - `GetRankingWithMyEntry()`: Top N 랭킹과 내 기록 분리 조회
  - 자동 기록 업데이트 (`OnBestTimeUpdated`, `OnBestDeathUpdated` 이벤트 구독)

- **[ItemDatabase](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Data/StaticData/ItemDatabase.cs)**  
  - `ItemData.asset` 목록 로드, 아이템 정의 등의 정적 데이터 정보 제공

### Infrastructure Layer
- **[ResourceManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/ResourceManager.cs)**  
  - `Resources.Load`/`Instantiate`/`Destroy` 래핑  

- **[UIManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/UIManager.cs)**  
  - `@UI_Root` 생성 → 씬(`UI_Scene`), 팝업(`UI_Popup`) UI 인스턴스화  
   - Canvas 세팅, 팝업 스택 관리, `Time.timeScale` 제어  
   - **Zenject DI 통합**: 런타임 UI 컴포넌트 자동 의존성 주입

- **[SceneManagerEX](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/SceneManagerEX.cs)**  
  - 씬 전환 전 `Managers.Clear()` → `SceneManager.LoadScene()`  

- **[SoundManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/SoundManager.cs)**  
  - BGM/SFX 풀 관리  

- **[PoolManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/PoolManager.cs)**  
  - 오브젝트 풀링 지원  

- **[GameManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/GameManager.cs)**  
  - 플레이 타이머·사망 카운트·체크포인트 관리  

- **[DataSyncManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/DataSyncManager.cs)**  
  - **델타 기반 실시간 데이터 동기화**
  - 주기적 배치 전송 (5초 간격)
  - 실패 시 재시도 메커니즘
  - 앱 종료 시 즉시 Flush 처리

- **[OfflineCacheManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Data/OfflineCacheManager.cs)**  
  - **네트워크 상태 감지 및 오프라인 캐싱**
  - 온라인 복귀 시 자동 동기화
  - `UI_SyncStatus`를 통한 동기화 상태 표시

- **[SaveManager](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Managers/SaveManager.cs)**  
  - 게임 종료 시 자동 저장 및 동기화 처리
  - `OnApplicationPause`, `OnApplicationFocus` 이벤트 처리

- **Utilities**  
  - [Define.cs](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Utils/Define.cs): 전역 enum/상수  
  - [Util.cs](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Utils/Util.cs): 컴포넌트 보장·계층 탐색  
  - [Extension.cs](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Utils/Extension.cs): `GameObject` 확장 메서드

### UI Layer
- **UI 계층화**  
  - [UI_Base](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/UI_Base.cs)
    - 모든 UI 컴포넌트의 공통 바인딩·초기화 로직 포함 (추상 클래스).  
    - **Zenject DI 통합**: `[Inject]` 어트리뷰트로 매니저 자동 주입

  - [UI_Scene](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Scene/UI_Scene.cs) : UI_Base  
    - 각 씬 전용 UI 진입점. `Init()` 추상화 → `Awake()` 시 자동 호출.  

  - [UI_Popup](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Popup/UI_Popup.cs) : UI_Base  
    - 팝업 UI 전용, 스택 기반 중첩·닫기, `Time.timeScale`·커서 제어.  

- **Scene–UI 호출 관계**  
  - [BaseScene](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Scenes/BaseScene.cs) (추상 MonoBehaviour)  
    - `Awake()` 에서 `Init()` 호출 → 가상 메서드 `Init()` 이 자식으로 dispatch  
    - `Clear()` 에서 팝업·씬 UI 정리  

  - **[MainScene](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Scenes/MainScene.cs)**, **[LobbyScene](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Scenes/LobbyScene.cs)**, **[StageScene](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Scenes/StageScene.cs)**  
    - `BaseScene` 상속  
    - `Init()` override 내부에서  
      ```csharp
      Managers.Instance.UI.ShowSceneUI<UI_Main>("UI_Main");
      // 또는 ShowSceneUI<UI_Lobby>, ShowSceneUI<UI_Stage>
      ```  
      을 호출해 해당 씬의 UI 진입점을 띄움  

- **주요 UI 컴포넌트**  
  - **Title Scene**: [`UI_Main`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Scene/UI_Main.cs), [`UI_Achievement`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Popup/UI_Achievement.cs), [`UI_Settings`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Popup/UI_Settings.cs), [`SelectCharacter`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/CharacterSelector.cs)  
  - **Lobby Scene**: [`UI_Lobby`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Scene/UI_Lobby.cs), [`UI_Shop`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Popup/UI_Shop.cs), [`UI_Warning`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Popup/UI_Warning.cs), `UI_SelectChapter`,  
    [`UI_SelectStage`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Popup/UI_SelectStage.cs), [`UI_GenericInfoPopup`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Popup/GenericInfoPopup.cs), **[`UI_Ranking`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Popup/UI_Ranking.cs)**  
  - **Stage Scene**: [`UI_Stage`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Scene/UI_Stage.cs), [`UI_Inventory`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Scene/UI_Inventory.cs), [`UI_Information`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Popup/UI_Information.cs), `UI_GenericInfoPopup`, [`UI_Result`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/UI/Popup/UI_Result.cs), `UI_Warning`

### Game Systems
 - **ItemSystem**: [`FeatherUse`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Items/FeatherUse.cs), [`WingUse`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Items/WingUse.cs), [`LampUse`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Items/LampUse.cs), [`FlagUse`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Items/FlagUse.cs)  
 - **ClimbingSystem**: 벽면 그랩·이동 FSM  
 - **ObstacleSystem**: 장애물 스폰·충돌 효과  
 - **InputSystem**: 키보드·게임패드 입력 처리 ([`ItemInput`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Items/ItemInput.cs) 등)

### Server-Side Architecture (ASP.NET Core)
- **[RankingController](https://github.com/heejune1209/Just-climb-/blob/main/Server/Server/Controllers/RankingController.cs)**: 랭킹 조회 및 기록 업데이트 API
- **[RankingService](https://github.com/heejune1209/Just-climb-/blob/main/Server/Server/Services/RankingService.cs)**: 랭킹 비즈니스 로직 처리
- **Entity Framework Core**: 데이터베이스 ORM 및 마이그레이션 관리
- **로깅 시스템**: 요청/응답 추적 및 에러 처리

[UI 시퀀스 다이어그램](https://github.com/heejune1209/Just-climb-/blob/main/UI%20%EC%8B%9C%ED%80%80%EC%8A%A4%20%EB%8B%A4%EC%9D%B4%EC%96%B4%EA%B7%B8%EB%9E%A8.md)

---
    
### 아이템·재화 시스템 구조
![image](https://github.com/user-attachments/assets/3e08867c-3aa3-4872-96d4-93a4b4a64304)

- **Definition Layer**: ScriptableObject 에셋(`ItemData.asset` + 개별 SO)  
- **Logic Layer**: [`ItemData.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Items/ItemData.cs)+[`IItemUse`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Items/IItemUse.cs) 인터페이스 → 개별 `(아이템 이름)Use.cs` 구현  
- **Data Layer**: `SaveData`·`InventoryItem` 클래스 + `DataManager` (JSON 입출력·이벤트)  
- **Domain Layer**: `ItemDatabase`, `ItemManager`, `CurrencyManager` (로직·이벤트)  
- **UI Layer**: `UI_Inventory` (아이템 슬롯 UI 표시)  

[아이템 시퀀스 다이어그램](https://github.com/heejune1209/Just-climb-/blob/main/%EC%95%84%EC%9D%B4%ED%85%9C%20%EC%8B%9C%ED%80%80%EC%8A%A4%20%EB%8B%A4%EC%9D%B4%EC%96%B4%EA%B7%B8%EB%9E%A8.md)

---

### Obstacle Structure 
![image](https://github.com/user-attachments/assets/24ebe925-cbaf-4006-8240-513eebafee46)
- **Core Interface & Base**  
  [`IObstacle.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Obstacles/Core/IObstacle.cs)(동작 계약), [`ObstacleBase.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Obstacles/Core/ObstacleBase.cs)(Activate/Deactivate 공통 로직), [`ObstacleTrigger.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Obstacles/Core/ObstacleTrigger.cs)(충돌 감지 → IObstacle 호출)로 모든 장애물의 기본 뼈대를 정의.

- **Obstacle Definitions**  
  `ObstacleData.asset`(공통 속성)과 `DropperData.asset`, `RollerData.asset`, `CannonData.asset` 같은 ScriptableObject에 개별 장애물 파라미터를 저장.

- **Spawner Components**  
  [`RockDropper.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Obstacles/Spawners/RockDropper.cs), [`RollingSpawner.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Obstacles/Spawners/RollingSpawner.cs), [`CannonShooter.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Obstacles/Spawners/CannonShooter.cs)가 정의된 Data Asset을 읽어 실제 장애물을 씬에 스폰하는 역할.

- **Obstacle Effects**  
  [`KnockbackZone.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Obstacles/Effects/KnockbackZone.cs), [`JumpPad.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Obstacles/Effects/JumpPad.cs), [`DeathZone.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Obstacles/Effects/DeathZone.cs), [`MaterialChanger.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Obstacles/Effects/MaterialChanger.cs) 등으로 장애물에 닿았을 때 발생할 충돌 반응이나 특수 효과를 구현.

- **Pooling Support**  
  [`PoolableObstacle.cs`](https://github.com/heejune1209/Just-climb-/blob/main/Assets/Scripts/Obstacles/Utils/PoolableObstacle.cs)는 Object Pooling 기능을 제공하며, `ObstacleBase`를 상속해 장애물 인스턴스를 재사용.  

---

### 랭킹 시스템 구조

- **클라이언트 측**
  - `RankingManager`: 서버 통신 및 캐싱 관리
  - `UI_Ranking`: 랭킹 UI 표시 및 정렬 옵션 제공
  - `StageManager` 이벤트 구독을 통한 자동 기록 업데이트

- **서버 측**
  - `RankingController`: REST API 엔드포인트
  - `RankingService`: 비즈니스 로직 및 데이터 처리
  - **Entity Framework Core**: 데이터베이스 ORM

- **주요 기능**
  - **실시간 랭킹**: 클리어 타임 및 사망 횟수 기준 정렬
  - **개인 기록 분리**: Top N 랭킹과 내 기록 별도 표시
  - **캐시 최적화**: 중복 요청 방지 및 성능 향상
  - **테스트 데이터**: 개발 및 테스트용 더미 데이터 생성

---

## 주요 기여

### ✅ **Zenject DI 아키텍처로 전환**
- **Service Locator → Dependency Injection 전환**으로 의존성 관리 개선
- **ProjectInstaller**를 통한 계층별 의존성 주입 체계 구축
- **인터페이스 분리**: 각 매니저별 `IManager` 인터페이스 정의로 테스트 용이성 확보
- **자동 초기화**: `IInitializable` 인터페이스로 매니저 초기화 순서 보장

### ✅ **메모리 누수 방지 시스템**
- **IDisposable 패턴**: 모든 매니저에 메모리 누수 방지 로직 구현
- **이벤트 해제**: 매니저 간 이벤트 구독 해제 자동화
- **리소스 정리**: HTTP 클라이언트, 세마포어 등 네이티브 리소스 정리
- **생명주기 관리**: Zenject의 DisposableManager와 연동한 자동 정리

### ✅ **실시간 랭킹 시스템 구현**
- **서버-클라이언트 구조**: ASP.NET Core Web API 기반 백엔드
- **다중 정렬 기준**: 클리어 타임, 사망 횟수 기준 랭킹 지원
- **실시간 동기화**: 기록 갱신 시 자동 서버 업데이트
- **캐시 최적화**: 중복 요청 방지 및 성능 향상
- **UI/UX 개선**: Top N 랭킹과 내 기록 분리 표시

### ✅ **온라인/오프라인 데이터 동기화**
- **델타 이벤트 시스템**: 데이터 변경 사항만 실시간 전송
- **배치 처리**: 5초 간격 주기적 동기화로 서버 부하 최적화
- **오프라인 캐싱**: 네트워크 단절 시 로컬 큐잉 후 복구 시 자동 동기화
- **UI 상태 표시**: 동기화 진행 상황 실시간 피드백

### ✅ UI/UX 시스템 제작
- `UI_Scene`, `UI_Popup` 구조 설계 및 자동화 슬롯 생성 툴 제작
- 이벤트 기반 구조로 UI 갱신을 분리하여 유지보수성과 확장성 강화
- **Zenject DI 통합**: UI 컴포넌트 자동 의존성 주입

### ✅ 아이템·재화 시스템 설계 및 구현
- ScriptableObject + 인터페이스 기반 구조로 설계
- **신규 아이템 추가 시 코드 수정 없이 에셋 등록만으로 반영 가능**
- **`CurrencyManager`/`ItemManager`**: `DataManager` 이벤트 구독 기반으로 골드·보석·아이템 수량 변경 시 `OnGoldChanged`·`OnItemCountChanged` 발행 → UI 자동 동기화

### ✅ 장애물 시스템 모듈화
- 장애물 **트리거 / 스폰 / 효과**를 명확히 분리하여 구조화
- 스폰 주기 및 파라미터를 **ScriptableObject**로 설정 가능하도록 유연하게 설계
- **풀링(Pooling)** 적용으로 실시간 낙석 및 발사 성능 최적화

### ✅ 클라이밍 시스템 분석 및 수정
- 기존 FSM 흐름 분석 후 **벽면 인식 로직 및 이동 제약 조건** 수정
- **경사면 처리 누락** 문제를 직접 해결하여 자연스러운 클라이밍 구현

### ✅ 데이터 관리 및 구조 리펙토링
- **PlayerPrefs → JSON** 전환: `DataManager`/`SaveData` 도입으로 `save.json` 기반 직렬화·역직렬화 구현
- **스테이지 메트릭(Metric)** 확장: 플레이 시간(`stagePlayTimes`), 사망 횟수(`stageDeathCounts`), 깃발 위치(`stageFlagPositions`), 최단 기록(`stageTimes`) 등 `SaveData`에 통합
- **델타 이벤트 시스템**: 데이터 변경 추적 및 실시간 동기화 기반 마련

### ✅ 스테이지 메트릭(Metric)·체크포인트 관리
- **`StageManager`**: 언락 플래그·최고 보상·최단 클리어 타임·최저 사망 횟수 이벤트 기반 갱신
- **`GameManager`**: 씬 로드/언로드 콜백으로 플레이 시간 복원·저장, 체크포인트(깃발) 위치 복원·저장

### ✅ 매니저 컨테이너 & 계층형 아키텍처
* **Zenject DI**: Persistence/Domain/Infrastructure/UI 전 매니저 의존성 주입 및 자동 초기화
* **Clear/Init 패턴**: 씬 전환 시 자동 정리, `BaseScene` 상속 구조로 `Init()` 강제 실행
* **4계층 분리**: Persistence·Domain·Infrastructure·UI 레이어 명확화.
  
### ✅ 캐릭터 능력치 밸런싱

---

## 🔧 **현재 진행중인 작업 내용**

### 🔐 **스팀 로그인 연동 시스템**
스팀 세션으로 자동 인증·식별 → JWT 발급

- **클라이언트**
  - **Steamworks.NET** 설치 및 초기화(`SteamAPI.Init()`)
  - **SteamAuthManager** 작성: SteamID, AuthTicket 획득 → `/api/auth/steam` POST → JWT 저장
  
- **서버 (ASP.NET Core Web API)**
  - **AuthController**: `POST /api/auth/steam` → Valve Web API 티켓 검증 → `IUserService.GetOrCreateAsync` → JWT 발급
  - **JWT 미들웨어** 설정 및 인증 체계 구축
  - **IUserService** / **UserService**: SteamID 기반 유저 레코드 관리
  
- **데이터베이스 (Entity Framework Core)**
  - **User Entity** 모델 정의 (SteamID를 Primary Key로 사용)
  - `Add-Migration CreateUsersTable` → Steam 프로필 정보 포함
  - **DbContext** 설정 및 의존성 주입

### 🏆 **업적 시스템 (+ Steam 업적 연동)**
게임 이벤트 → 서버 UPSERT & Steam 서버에도 업적 언락

- **클라이언트**
  - **AchievementManager** 달성 로직: `GenerateDelta("achievement_unlocked", achId)` + Steamworks.NET `SteamUserStats.SetAchievement`, `StoreStats()`
  - **UI_AchievementPopup** 구현: 업적 달성 시 시각적 피드백
  
- **서버 (ASP.NET Core Web API)**
  - **AchievementController**: `GET /api/users/{uid}/achievements`, `POST /api/users/{uid}/achievements`
  - **IUserAchievementService** + **UserAchievementService**: 델타 병합·UPSERT
  
- **데이터베이스 & 캐시 (Entity Framework Core)**
  - **Achievement Entity** 모델 정의
  - `Add-Migration CreateAchievements` → 업적 테이블 생성
  - **Repository 패턴**: EF Core 기반 업적 UPSERT 로직
  - Redis 캐시 연동

### 🎭 **캐릭터 선택 시스템**
클라이언트 UI → 서버 저장 → 모든 기기에서 동기화

- **클라이언트**
  - **CharacterSelectManager**: UI 버튼 클릭 → `GenerateDelta("selectedCharacter", charId)` 또는 `CharacterService.SetSelectedAsync` 호출
  - **UI_CharacterSelect** 구현: 캐릭터 프리뷰 및 선택 인터페이스
  
- **서버 (ASP.NET Core Web API)**
  - **CharacterController**: `GET /api/users/{uid}/character`, `PUT /api/users/{uid}/character`
  - **IUserCharacterService** + **UserCharacterService**: 델타 병합·UPSERT
  
- **데이터베이스 (Entity Framework Core)**
  - `Add-Migration AddSelectedCharacterColumn` → User 엔티티에 SelectedCharacter 속성 추가
  - **CharacterHistory Entity**: 캐릭터 변경 이력 추적을 위한 엔티티 모델
  - **Repository 패턴**: EF Core 기반 캐릭터 데이터 관리


### 🚀 **전체 파이프라인 통합 목표**
1. **클라이언트**: 로컬 JSON → Δ(델타) 생성 → `DataSyncManager` 주기 전송/재시도 → UI 표시
2. **서버**: ASP.NET Core Web API → 스팀 인증·검증 → `ConflictResolver` → DB/Redis 반영  
3. **DB/Redis**: 정규화 테이블 + 캐시 로직 → 실시간 랭킹·상태 조회
4. **Steam 연동**: 로그인 인증 + 업적 동기화로 완전한 스팀 게임 경험 제공

---

## 관련 링크
### 시연 동영상
https://youtu.be/HkNdxTHhVaw?si=IY2KK07vDpRfTPzP

