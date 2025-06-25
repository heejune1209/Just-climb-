아래와 같이 주요 **5개 기능**(하이브리드 델타 동기화, 스팀 로그인, 랭킹, 업적, 캐릭터 선택) 별로 **클라이언트 · 서버 · DB/캐시** 관점에서 해야 할 작업(Task List)을 정리했습니다.

---

## 1. 하이브리드 델타 동기화

온라인/오프라인 모드에 따라 로컬 JSON ↔ 서버 DB 동기화를 관리

| 레이어       | 작업 내용                                                                                                                                              | 파일/클래스                                        |
| --------- | -------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------- |
| **클라이언트** | • `DataManager.Load()` 리팩토링: 로컬 JSON 읽기 → 네트워크 연결 시 서버 GET → JSON 덮어쓰기<br>• `DataManager.Save()` 리팩토링: 로컬 JSON 쓰기 → 온라인일 때 `DataSyncManager` 델타 큐잉 | `DataManager.cs`                              |
|           | • `OfflineCacheManager` 강화: 네트워크 상태 감지 → 온라인 복귀 시 로컬 델타 일괄 전송                                                                                      | `OfflineCacheManager.cs`                      |
|           | • `DataSyncManager` 유지: 델타 큐잉·POST·재시도 로직                                                                                                          | `DataSyncManager.cs`                          |
| **서버**    | • `SaveController` GET/POST 엔드포인트 구현: `/api/users/{uid}/state`, `/api/users/{uid}/state/delta`<br>• `IUserStateService.LoadStateAsync` 추가          | `SaveController.cs`<br>`IUserStateService.cs` |
|           | • `UserStateService`: 델타 병합·UPSERT·트랜잭션·Redis 갱신                                                                                                   | `UserStateService.cs`                         |
|           | • `ConflictResolver` 델타 충돌 처리                                                                                                                      | `ConflictResolver.cs`                         |
| **DB/캐시** | • `Migration_CreateUsersTable.sql`, `Migration_CreateUserItemsTable.sql`<br>• `UpsertUserItem.sql`<br>• `RedisSyncConfig.json`                     | `/Database/Migrations`<br>`/Config`           |

---

## 2. 스팀 로그인 연동

스팀 세션으로 자동 인증·식별 → JWT 발급

| 레이어       | 작업 내용                                                                                                                             | 파일/클래스                                |
| --------- | --------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------- |
| **클라이언트** | • Steamworks.NET 설치 및 초기화(`SteamAPI.Init()`)<br>• `SteamAuthManager` 작성: SteamID, AuthTicket 획득 → `/api/auth/steam` POST → JWT 저장 | `SteamAuthManager.cs`                 |
| **서버**    | • `AuthController`: `POST /api/auth/steam` → Valve Web API 티켓 검증 → `IUserService.GetOrCreateAsync` → JWT 발급<br>• JWT 미들웨어 설정      | `AuthController.cs`<br>`Program.cs`   |
|           | • `IUserService` / `UserService`: SteamID 기반 유저 레코드 관리                                                                            | `IUserService.cs`<br>`UserService.cs` |
| **DB**    | • `Migration_CreateUsersTable.sql` (SteamID를 `users.id` 로 사용)<br>• (필요시) `users` 테이블에 Steam 프로필 컬럼 추가                             | `/Database/Migrations`                |

---

## 3. 랭킹 시스템

서버에서 Top N, 내 순위 조회 → 클라 UI 표시

| 레이어       | 작업 내용                                                                                                                                                                                               | 파일/클래스                                                                           |
| --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| **클라이언트** | • `IApiClient.GetAsync<T>` / `ApiClient` 작성<br>• `IRankingService` + `RankingService` 구현: `GetTopRankingAsync`, `GetMyRankAsync`<br>• `UI_Ranking` 수정: Top 20 표시, 내 순위 하단 강조                        | `ApiClient.cs`<br>`IRankingService.cs`<br>`RankingService.cs`<br>`UI_Ranking.cs` |
| **서버**    | • `RankingController`:<br> → `GET /api/rankings/{stage}` (Top N 리턴)<br> → `GET /api/rankings/{stage}/my/{uid}`<br> • `IUserRankingService` + `UserRankingService`:<br> → Redis Sorted Set UPSERT/조회 | `RankingController.cs`<br>`IUserRankingService.cs`<br>`UserRankingService.cs`    |
| **DB/캐시** | • Redis Sorted Set (`rankings:{stage}`)<br>• (옵션) `Migration_CreateRankingsTable.sql`                                                                                                               | Redis 설정, (테이블 스키마)                                                              |

---

## 4. 업적 시스템 (+ Steam 업적 연동)

게임 이벤트 → 서버 UPSERT & Steam 서버에도 업적 언락

| 레이어       | 작업 내용                                                                                                                                                                           | 파일/클래스                                                                                    |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| **클라이언트** | • `AchievementManager`에 달성 로직 추가: `GenerateDelta("achievement_unlocked", achId)` + Steamworks.NET `SteamUserStats.SetAchievement`, `StoreStats()`<br>• `UI_AchievementPopup` 구현 | `AchievementManager.cs`<br>`UI_AchievementPopup.cs`                                       |
| **서버**    | • `AchievementController`: `GET /api/users/{uid}/achievements`, `POST /api/users/{uid}/achievements`<br>• `IUserAchievementService` + `UserAchievementService`: 델타 병합·UPSERT    | `AchievementController.cs`<br>`IUserAchievementService.cs`<br>`UserAchievementService.cs` |
| **DB/캐시** | • `Migration_CreateAchievementsTable.sql`<br>• `UpsertUserAchievement.sql`<br>• `RedisSyncConfig.json`                                                                          | `/Database/Migrations`<br>`/Database/Scripts`<br>`/Config`                                |

---

## 5. 캐릭터 선택 시스템

클라 UI → 서버 저장 → 모든 기기에서 동기화

| 레이어       | 작업 내용                                                                                                                                                           | 파일/클래스                                                                              |
| --------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| **클라이언트** | • `CharacterSelectManager` 구현: UI 버튼 클릭 → `GenerateDelta("selectedCharacter", charId)` 또는 `CharacterService.SetSelectedAsync` 호출<br>• `UI_CharacterSelect` 구현   | `CharacterSelectManager.cs`<br>`UI_CharacterSelect.cs`                              |
| **서버**    | • `CharacterController`: `GET /api/users/{uid}/character`, `PUT /api/users/{uid}/character`<br>• `IUserCharacterService` + `UserCharacterService`: 델타 병합·UPSERT | `CharacterController.cs`<br>`IUserCharacterService.cs`<br>`UserCharacterService.cs` |
| **DB**    | • `Migration_AddSelectedCharacterColumn.sql`                                                                                                                    | `/Database/Migrations`                                                              |

---

이 Task List를 따라
1️⃣ **순차적 구현** (클라 ↔ 서버 ↔ DB) →
2️⃣ **통합 테스트** (오프라인→온라인 전환, Steam 로그인, 랭킹/업적/캐릭터)\*\* →
3️⃣ **배포(스팀 빌드 & 서버 PaaS)** 하시면, 스팀 출시에 필요한 모든 기능이 완성됩니다.

--- 
DTO 관점에서 **SaveData** 와 **DeltaEvent** 가 각각 **오프라인**·**온라인** 모드에서 어떻게 활용되는지 정리해 드릴게요.

---

## 1. 오프라인 모드

### SaveData (풀 상태 캐시)

* **LoadAsync()** 시작 시

  * 로컬에 남아 있는 `save.json` 파일을 읽어 `SaveData` 객체로 역직렬화
  * `Current`에 로드하여 게임 진행에 사용
* **Save()** 호출 시

  * 게임 상태(`Current`)를 JSON으로 직렬화해 로컬 디스크에 즉시 저장
  * 앱 강제 종료나 크래시 발생하더라도 마지막 상태가 유실되지 않도록 보장

### DeltaEvent (변경 사항 임시 저장)

* 특정 필드가 바뀔 때마다 `GenerateDelta(key, value)`가 호출되어

  * 새로운 `DeltaEvent(key, valueJson, timestamp)` 인스턴스가 생성
  * `OfflineCacheManager`가 이를 받아 내부 리스트(`_offlineQueue`)에 보관
* 네트워크가 복구될 때까지 **전송하지 않고** 오직 로컬 큐에만 쌓아두기

---

## 2. 온라인 모드

### 초기 동기화 (풀 상태 GET)

1. **LoadAsync()** 에서 로컬 JSON을 먼저 로드 후
2. `Application.internetReachability` 확인 → **서버 GET**

   * `GET /api/users/{uid}/state` 호출
   * 응답받은 전체 `SaveData` JSON으로 로컬 파일 덮어쓰기 + `Current` 갱신
3. 이후 게임은 서버에서 가져온 최신 `Current` 상태로 계속 동작

### 변경 사항 전송 (델타 POST)

1. 게임 중 필드 변경 시

   * `GenerateDelta()` → `OnDeltaGenerated` → `DataSyncManager.EnqueueDelta`
2. **DataSyncManager** 가 주기적으로, 혹은 온라인 전환 시

   * `_queue` 에 쌓인 `DeltaEvent` 리스트를 묶어 `POST /api/users/{uid}/state/delta` 로 전송
3. 서버는 받은 델타를 `IUserStateService.MergeDeltasAsync()` 로 병합(UPSERT)
4. 성공 응답 → `DeltaEvent` 는 큐에서 제거, 실패 시 재시도

---

### 요약

| 모드       | SaveData 활용                           | DeltaEvent 활용                    |
| -------- | ------------------------------------- | -------------------------------- |
| **오프라인** | • 로컬 JSON 캐시로만 로드/저장                  | • 생성된 델타를 로컬 큐에 보관               |
| **온라인**  | • 서버 GET → 전체 `SaveData` 갱신 → 로컬 덮어쓰기 | • 큐에 쌓인 델타를 서버 POST → 병합(UPSERT) |

이렇게 **SaveData** 와 **DeltaEvent** 두 DTO를 조합해,

* 오프라인에서도 안정적 플레이와 캐싱을 보장하고,
* 온라인 복귀 시 최소한의 데이터(델타)만 전송하여 서버 DB와 완벽히 동기화하는
  하이브리드 델타 동기화 구조가 구현됩니다.

