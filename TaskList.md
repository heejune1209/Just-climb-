아래와 같이 “스팀 출시용”으로 **업적**, **랭킹**, **캐릭터 선택**, 그리고 **델타 기반 동기화 시스템**까지 네 가지 영역별로, 클라이언트·서버·DB/캐시에 각각 어떤 스크립트·설정을 준비해야 할지 TaskList 형태로 정리해 보았습니다.

---

## 1. 업적 (Achievements)

| 계층          | 파일/클래스                                                                  | 역할                                                                   |
| ----------- | ----------------------------------------------------------------------- | -------------------------------------------------------------------- |
| **클라이언트**   | `AchievementManager.cs`<br>`IInitializable`                             | 게임 이벤트 구독 → 달성 조건 체크 → 달성 처리 → 델타 생성                                 |
|             | `UI_AchievementPopup.cs`<br>`UI_Popup`                                  | 업적 언락 시 팝업 표시                                                        |
|             | (DataManager 호출)                                                        | `_dataManager.GenerateDelta("achievement_unlocked", achievementId)`  |
| **서버**      | `AchievementController.cs`<br>`ApiController`                           | `/api/users/{uid}/achievements` 델타 수신 → `IUserAchievementService` 호출 |
|             | `IUserAchievementService.cs` / `UserAchievementService.cs`              | 델타 병합·UPSERT 로직                                                      |
|             | `ConflictResolver.cs`                                                   | 업적 충돌 해결 (중복 언락 방지 등)                                                |
| **DB & 캐시** | `Migration_CreateAchievementsTable.sql`<br>`Server/Database/Migrations` | `user_achievements` 테이블 생성                                           |
|             | `UpsertUserAchievement.sql`                                             | UPSERT 프로시저 / 스크립트                                                   |
|             | `RedisSyncConfig.json`                                                  | 업적 델타 캐시 설정                                                          |

---

## 2. 랭킹 (Ranking)

| 계층          | 파일/클래스                                             | 역할                                                                               |
| ----------- | -------------------------------------------------- | -------------------------------------------------------------------------------- |
| **클라이언트**   | `RankingManager.cs`<br>`IRankingManager`           | 로컬 Top N 관리 → 서버 API 호출 → 델타 전송                                                  |
|             | `UI_Ranking.cs` / `UI_RankingEntry.cs`             | 글로벌 Top N 표시, `GetRanking(stage)` + `GetMyRankAsync(stage)`                      |
|             | (Networking)                                       | `UnityWebRequest` 또는 `HttpClient` 로 `/api/rankings` 호출                           |
| **서버**      | `RankingController.cs`<br>`ApiController`          | `GET /rankings/{stage}` → Top N 반환<br>`GET /rankings/{stage}/my/{uid}` → 내 순위 반환 |
|             | `IUserRankingService.cs` / `UserRankingService.cs` | Redis Sorted Set UPSERT + 조회 로직                                                  |
|             | `ConflictResolver.cs`                              | 동시성 충돌 해결 (동일 순위 처리 등)                                                           |
| **DB & 캐시** | (주로 Redis)                                         | `rankings:{stage}` Sorted Set 생성 / TTL, 인덱스 설정                                   |
|             | *(옵션)* `Migration_CreateRankingsTable.sql`         | DB에 저장할 경우 테이블 생성                                                                |

---

## 3. 캐릭터 선택 (Character Selection)

| 계층          | 파일/클래스                                                                     | 역할                                                                          |
| ----------- | -------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| **클라이언트**   | `CharacterSelectManager.cs`<br>`IInitializable`                            | UI 내 캐릭터 선택 → `DataManager.Current.selectedCharacter` 변경 → `Save()` + 델타 생성 |
|             | `UI_CharacterSelect.cs` / 프리팹                                              | 선택 화면 렌더링, 버튼 바인딩                                                           |
| **서버**      | `CharacterController.cs`<br>`ApiController`                                | `/api/users/{uid}/character` GET/PUT 처리                                     |
|             | `IUserCharacterService.cs` / `UserCharacterService.cs`                     | 델타 병합·UPSERT (selectedCharacter 필드)                                         |
|             | `ConflictResolver.cs`                                                      | 동시성 충돌 해결 (동일 선택 방지 등)                                                      |
| **DB & 캐시** | `Migration_AddSelectedCharacterColumn.sql`<br>`Server/Database/Migrations` | `users.selectedCharacter` 컬럼 추가                                             |
|             | `RedisSyncConfig.json`                                                     | 캐릭터 델타 캐시 설정                                                                |

---

## 4. 델타 기반 동기화 시스템 (Delta Sync)

| 계층          | 파일/클래스                                                                                | 역할                                                            |
| ----------- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------- |
| **클라이언트**   | `DataManager.cs`<br>`DeltaEvent.cs` / `OfflineCacheManager.cs` / `DataSyncManager.cs` | 델타 생성·큐잉·전송·재시도 로직                                            |
|             | `UI_SyncStatus.cs`                                                                    | 동기화 상태 UI 표시                                                  |
| **서버**      | `SaveController.cs`<br>`ApiController`                                                | `/api/users/{uid}/state/delta` 델타 수신 → `IUserStateService` 호출 |
|             | `IUserStateService.cs` / `UserStateService.cs`                                        | 델타 병합·UPSERT 로직<br>DB 트랜잭션, `ConflictResolver` 호출 포함          |
|             | `ConflictResolver.cs`                                                                 | 델타 충돌 해결                                                      |
| **DB & 캐시** | `Migration_CreateUsersTable.sql`<br>`Server/Database/Migrations`                      | `users` 테이블 생성                                                |
|             | `Migration_CreateUserItemsTable.sql` / `UpsertUserItem.sql`                           | `user_items` UPSERT 스크립트                                      |
|             | `RedisSyncConfig.json`                                                                | 델타 캐시 설정                                                      |

---

이 TaskList를 기반으로, **하나씩 구현 → 배포 → 테스트** 순으로 진행하시면 스팀 출시를 위한 업적·랭킹·캐릭터 선택·전체 동기화 시스템 준비가 완료됩니다. 부족한 부분 있으면 언제든 알려 주세요!

네, 가능합니다. Unity 쪽에서 **Steamworks.NET** 같은 Steamworks SDK 래퍼를 사용해, 게임 내 업적 달성 시 Steam 업적도 함께 언락하도록 연동할 수 있습니다.

---

## 1. Steam 업적 준비 (Steamworks 대시보드)

1. Steamworks 파트너 사이트에 접속 → **애플리케이션 관리** → **업적**
2. 각 업적에 **API Name**(예: `ACH_TUTORIAL_COMPLETE`, `ACH_FIRST_KILL` 등)과 **설명**, 아이콘을 등록
3. 저장 후 **업적 목록**이 Steam 클라이언트에도 자동 반영됩니다

> **Tip**: API Name 은 코드에서 호출할 때 정확히 일치시켜야 합니다.

---

## 2. Unity 프로젝트에 Steamworks.NET 설치

1. [Steamworks.NET GitHub](https://github.com/rlabrecque/Steamworks.NET) 에서 최신 릴리즈 ZIP 다운로드
2. Unity **패키지 매니저**(또는 Assets → Import Package → Custom Package) 로 `Steamworks.NET.unitypackage` 임포트
3. **Plugins** 폴더에 Steam API 라이브러리가 들어옵니다.

---

## 3. Steam API 초기화

`GameManager` 같은 전역 싱글톤 클래스의 `Awake()` 또는 초기화 파트에 추가:

```csharp
using Steamworks;

public class SteamInitializer : MonoBehaviour
{
    private void Awake()
    {
        if (!Packsize.Test()) Debug.LogError("Steamworks packsize mismatch");
        if (!DllCheck.Test())   Debug.LogError("Steamworks dll mismatch");

        if (SteamAPI.RestartAppIfNecessary((AppId_t)YOUR_STEAM_APP_ID))
        {
            Application.Quit();
            return;
        }

        SteamAPI.Init();  // Steam 초기화
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        SteamAPI.RunCallbacks();  // 콜백 처리
    }

    private void OnDestroy()
    {
        SteamAPI.Shutdown();
    }
}
```

* `YOUR_STEAM_APP_ID` 는 Steamworks 파트너 대시보드에서 받은 AppID

---

## 4. 업적 달성 시 Steam에도 언락

기존 `AchievementManager.cs` 에서 업적을 언락하는 부분에 다음 코드를 추가합니다:

```csharp
using Steamworks;

public class AchievementManager : IInitializable
{
    // … 기존 의존성 주입, 컨디션 체크 등 …

    private void UnlockAchievement(string achievementKey)
    {
        // 1) 로컬 서버 동기화용 델타
        _dataManager.GenerateDelta("achievement_unlocked", achievementKey);

        // 2) Steam 업적 언락
        if (SteamManager.Initialized)  // Steamworks.NET 초기화 확인
        {
            bool alreadyUnlocked;
            SteamUserStats.GetAchievement(achievementKey, out alreadyUnlocked);
            if (!alreadyUnlocked)
            {
                SteamUserStats.SetAchievement(achievementKey);
                SteamUserStats.StoreStats();  // 변경 정보 서버로 전송
                Debug.Log($"Steam Achievement Unlocked: {achievementKey}");
            }
        }
    }

    public void OnSomeGameEvent(...)
    {
        if (/* 언락 조건 만족 */)
            UnlockAchievement("ACH_TUTORIAL_COMPLETE"); 
    }
}
```

* `SteamUserStats.GetAchievement` 로 이미 언락되었는지 체크
* `SetAchievement` + `StoreStats` 로 Steam 서버에 저장

---

## 5. 인스펙터 바인딩 및 빌드 설정

* **SteamInitializer**: 씬 첫 로딩 씬(메인 메뉴)에 붙여 두고,
  `YOUR_STEAM_APP_ID` 만 인스펙터나 상수로 설정
* **AchievementManager**: Zenject 등 DI로 바인딩
* **플랫폼 설정**:

  * **PC, Windows** 플랫폼에서만 Steamworks 초기화하도록 빌드
  * **Editor** 에서는 `#if UNITY_STANDALONE_WIN` 등으로 분기

---

### 요약

1. Steamworks 대시보드에 업적 정의 → `API Name` 확보
2. Unity에 Steamworks.NET 설치 → 초기화 스크립트 추가
3. `SetAchievement` & `StoreStats` 호출로 업적 언락
4. 기존 델타 동기화 로직(`GenerateDelta`)과 함께 실행

이렇게 하면 게임 내에서 업적을 달성하자마자, 스팀 클라이언트의 업적 창에도 바로 반영되어 뜹니다.

