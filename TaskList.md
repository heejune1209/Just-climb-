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
