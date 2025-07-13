# UI 시퀀스 다이어그램

## 1) 씬 초기화 및 UI 로드 플로우 (BaseScene + Zenject DI)

```mermaid
sequenceDiagram
    participant Unity as Unity Engine
    participant LobbyScene as LobbyScene<br/>(BaseScene 상속)
    participant Zenject as Zenject Container
    participant UIManager as UIManager
    participant UI_Lobby as UI_Lobby<br/>(UI_Scene)
    participant ResourceMgr as ResourceManager

    %% 씬 로드 및 초기화
    Unity->>LobbyScene: Scene Load
    LobbyScene->>LobbyScene: Awake()
    LobbyScene->>LobbyScene: Init() (가상 메서드)
    
    %% Zenject DI 의존성 주입
    Note over LobbyScene, Zenject: Zenject DI 컨테이너 의존성 주입
    Zenject->>UIManager: [Inject] UIManager 주입
    Zenject->>ResourceMgr: [Inject] ResourceManager 주입
    
    %% UI 생성 및 초기화
    LobbyScene->>UIManager: ShowSceneUI<UI_Lobby>("UI_Lobby")
    UIManager->>ResourceMgr: Load<UI_Lobby>("UI_Lobby")
    ResourceMgr-->>UIManager: UI_Lobby Prefab
    UIManager->>UI_Lobby: Instantiate & [Inject] 의존성 주입
    UIManager->>UI_Lobby: Init() 호출
    
    Note over UI_Lobby: UI_Base → UI_Scene 계층 구조
    UI_Lobby-->>LobbyScene: UI 초기화 완료
```

## 2) 팝업 시스템 플로우 (UI_Scene → UI_Popup)

```mermaid
sequenceDiagram
    participant Player as 플레이어
    participant UI_Lobby as UI_Lobby<br/>(UI_Scene)
    participant UIManager as UIManager
    participant UI_Shop as UI_Shop<br/>(UI_Popup)
    participant CurrencyMgr as CurrencyManager
    participant ItemMgr as ItemManager

    %% 상점 팝업 열기
    Player->>UI_Lobby: Shop 버튼 클릭
    UI_Lobby->>UIManager: ShowPopupUI<UI_Shop>("UI_Shop")
    UIManager->>UI_Shop: Instantiate & [Inject] 의존성 주입
    
    Note over UI_Shop: UI_Base → UI_Popup 계층
    UIManager->>UI_Shop: Init() & 팝업 스택 추가
    UI_Shop->>CurrencyMgr: GetGold(), GetGems()
    UI_Shop->>ItemMgr: GetAllItemIds()
    UI_Shop-->>Player: 상점 UI 표시

    %% 아이템 구매
    Player->>UI_Shop: 아이템 구매 버튼 클릭
    UI_Shop->>CurrencyMgr: SpendGold(price)
    CurrencyMgr->>ItemMgr: AddItem(itemType, count)
    
    Note over CurrencyMgr, ItemMgr: 이벤트 기반 UI 업데이트
    CurrencyMgr->>UI_Shop: OnGoldChanged(newAmount)
    ItemMgr->>UI_Shop: OnItemCountChanged(itemType, newCount)

    %% 팝업 닫기
    Player->>UI_Shop: ESC 키 또는 X 버튼
    UI_Shop->>UIManager: ClosePopupUI(this)
    UIManager->>UIManager: 팝업 스택에서 제거
    UIManager->>UI_Shop: Destroy()
```

## 3) Steam 인증 및 업적 시스템 플로우

```mermaid
sequenceDiagram
    participant Player as 플레이어
    participant UI_Main as UI_Main<br/>(메인 씬)
    participant SteamAuthMgr as SteamAuthManager
    participant AchievementMgr as AchievementManager
    participant UI_Achievement as UI_Achievement<br/>(팝업)
    participant UI_SyncStatus as UI_SyncStatus

    %% Steam 인증 과정
    UI_Main->>SteamAuthMgr: 게임 시작 시 자동 인증 시도
    SteamAuthMgr->>SteamAuthMgr: Steam 티켓 생성 및 서버 전송
    
    alt 인증 성공
        SteamAuthMgr->>UI_Main: OnAuthenticationSuccess(jwtToken)
        UI_Main->>UI_SyncStatus: 동기화 상태 "연결됨" 표시
        UI_Main-->>Player: Steam 사용자명 표시
    else 인증 실패
        SteamAuthMgr->>UI_Main: OnAuthenticationFailed(error)
        UI_Main->>UI_SyncStatus: "오프라인 모드" 표시
    end

    %% 업적 달성 플로우
    Note over Player, AchievementMgr: 게임 중 업적 달성 조건 충족
    AchievementMgr->>AchievementMgr: OnStageCleared() 이벤트 감지
    AchievementMgr->>AchievementMgr: 업적 달성 체크 및 Steam 업적 동기화
    AchievementMgr->>UI_Achievement: ShowAchievementPopup(achievementData)
    
    UI_Achievement-->>Player: 업적 달성 팝업 표시
    
    %% 업적 목록 보기
    Player->>UI_Main: 업적 버튼 클릭
    UI_Main->>UIManager: ShowPopupUI<UI_Achievement>("UI_Achievement")
    UIManager->>UI_Achievement: [Inject] AchievementManager 주입
    UI_Achievement->>AchievementMgr: GetAllAchievements()
    AchievementMgr-->>UI_Achievement: 업적 목록 데이터
    UI_Achievement-->>Player: 업적 목록 UI 표시
```

## 4) 랭킹 시스템 UI 플로우

```mermaid
sequenceDiagram
    participant Player as 플레이어
    participant UI_Lobby as UI_Lobby
    participant UIManager as UIManager
    participant UI_Ranking as UI_Ranking<br/>(팝업)
    participant RankingMgr as RankingManager
    participant UI_RankingEntry as UI_RankingEntry

    %% 랭킹 팝업 열기
    Player->>UI_Lobby: 랭킹 버튼 클릭
    UI_Lobby->>UIManager: ShowPopupUI<UI_Ranking>("UI_Ranking")
    UIManager->>UI_Ranking: [Inject] RankingManager 주입
    UI_Ranking->>UI_Ranking: Init() & 기본 스테이지 1 랭킹 로드

    %% 서버에서 랭킹 데이터 가져오기
    UI_Ranking->>RankingMgr: GetRankingWithMyEntry(stageNum, sortType)
    
    alt 캐시된 데이터 있음
        RankingMgr-->>UI_Ranking: 캐시된 랭킹 데이터 즉시 반환
    else 서버 요청 필요
        RankingMgr->>RankingMgr: HTTP API 서버 요청
        RankingMgr-->>UI_Ranking: 서버 랭킹 데이터 반환
    end

    %% UI 업데이트
    loop 각 랭킹 엔트리별
        UI_Ranking->>UI_RankingEntry: CreateRankingEntry(rankingData)
        UI_RankingEntry-->>UI_Ranking: 랭킹 엔트리 UI 생성
    end
    
    UI_Ranking-->>Player: 랭킹 목록 표시

    %% 스테이지 변경
    Player->>UI_Ranking: 다른 스테이지 탭 클릭
    UI_Ranking->>RankingMgr: GetRankingWithMyEntry(newStageNum, sortType)
    RankingMgr-->>UI_Ranking: 새 스테이지 랭킹 데이터
    UI_Ranking->>UI_Ranking: UI 리프레시
```

## 5) 데이터 동기화 상태 UI 플로우

```mermaid
sequenceDiagram
    participant DataMgr as DataManager
    participant DataSyncMgr as DataSyncManager
    participant OfflineCacheMgr as OfflineCacheManager
    participant UI_SyncStatus as UI_SyncStatus
    participant Player as 플레이어

    %% 데이터 변경 및 동기화 시작
    Note over DataMgr: 게임 데이터 변경 (골드 획득 등)
    DataMgr->>DataSyncMgr: OnDeltaGenerated(deltaEvent)
    DataSyncMgr->>UI_SyncStatus: 동기화 시작 상태 표시
    UI_SyncStatus-->>Player: "동기화 중..." 표시

    alt 온라인 상태
        DataSyncMgr->>DataSyncMgr: 서버로 델타 이벤트 전송
        
        alt 동기화 성공
            DataSyncMgr->>UI_SyncStatus: 동기화 성공 상태
            UI_SyncStatus-->>Player: "동기화 완료" 표시 (잠시 후 사라짐)
        else 동기화 실패
            DataSyncMgr->>OfflineCacheMgr: 오프라인 캐시에 저장
            OfflineCacheMgr->>UI_SyncStatus: 오프라인 캐시 상태
            UI_SyncStatus-->>Player: "오프라인 - 재연결 시 동기화" 표시
        end
        
    else 오프라인 상태
        DataSyncMgr->>OfflineCacheMgr: 오프라인 캐시에 저장
        OfflineCacheMgr->>UI_SyncStatus: 오프라인 모드 상태
        UI_SyncStatus-->>Player: "오프라인 모드" 표시
    end

    %% 네트워크 복구 시 자동 동기화
    Note over OfflineCacheMgr: 네트워크 연결 복구 감지
    OfflineCacheMgr->>DataSyncMgr: FlushCachedData()
    DataSyncMgr->>UI_SyncStatus: 캐시 동기화 시작
    UI_SyncStatus-->>Player: "데이터 동기화 중..." 표시
    
    DataSyncMgr->>UI_SyncStatus: 모든 캐시 동기화 완료
    UI_SyncStatus-->>Player: "동기화 완료" 표시
```

## 주요 특징

### 🏗️ **아키텍처 반영사항**
- **BaseScene 상속 구조**: `Awake()` → `Init()` 가상 메서드 호출 패턴
- **Zenject DI 통합**: `[Inject]` 어트리뷰트를 통한 자동 의존성 주입
- **UI 계층화**: UI_Base → UI_Scene/UI_Popup 상속 구조
- **이벤트 기반 UI 업데이트**: 매니저들의 이벤트 구독으로 UI 자동 갱신

### 🔄 **실시간 동기화**
- **델타 이벤트 시스템**: 변경사항만 추적하여 서버 전송
- **오프라인 캐싱**: 네트워크 단절 시 로컬 저장 후 복구 시 자동 동기화
- **UI 상태 표시**: 동기화 진행 상황 실시간 피드백

### 🎮 **Steam 통합**
- **자동 인증**: 게임 시작 시 Steam 세션 확인 및 서버 인증
- **업적 시스템**: 게임 내 업적과 Steam 업적 동시 달성
- **사용자 경험**: Steam 프로필 정보 표시 및 연동 상태 안내
