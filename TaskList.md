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
