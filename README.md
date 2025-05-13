## Just Climb!
🗓️ 프로젝트 개요
- 기간 : 2023.09 ~ 2023.12
- 인원 : 4인 (기획 1, 아트1, 프로그래머 2)
- 역할 : 메인 프로그래머
- 도구 : Unity3D, C#, Github
- 장르 : 어드벤처, 클라이밍, 3인칭 백뷰 
- 플랫폼 : PC

## 프로젝트 설명
- Unity 엔진 기반 3D 백뷰 클라이밍 게임
- 홀드를 이용한 암벽 등반과 장애물을 파훼하여 산 정상에 오르는 게임
- 총 8개 Stage 구성

## 설계서
### Game Flow
![Image](https://github.com/user-attachments/assets/679a1411-5d48-4aaa-879f-68a8efc2cd31)
- 씬 전환 기반 구조로 타이틀 → 로비 → 스테이지 → 결과로 이어지는 흐름
- 각 Scene은 UI 구조 및 매니저 관리 하에 독립적으로 동작.

### Game Structure
![image](https://github.com/user-attachments/assets/3bc31f13-ce7d-4ea4-a8c3-dd6282ab95d6)
### 주요 구성 요소
- **UI 계층화**  
  - `UI_Base` → `UI_Scene` 상속 구조로 화면(Scene) 단위와 팝업(Popup) 단위 로직을 분리  
  - `UI_Main`, `UI_Lobby`, `UI_Stage` 등 Scene별 진입점과  
    `UI_Settings`, `UI_Shop`, `UI_Inventory` 등 팝업/세부 UI로 구성

- **전체 게임 매니저 (Managers)**  
  - `SceneManagerEX`, `SoundManager`, `ResourceManager`, `UIManager`, `GameManager`, `PoolManager`, `ItemManager`
  - 전역 상태(씬 전환, 사운드, 리소스, UI 팝업, 게임 타이머/체크포인트, 오브젝트 풀 등) 일괄 관리

- **Stage System**  
  - `ItemSystem`, `ClimbingSystem`, `ObstacleSystem`, `InputSystem`  
  - 게임 플레이의 핵심 기능을 모듈화하여 유지·보수성 및 확장성 확보

- **Utilities (Helper) 클래스**  
  - `Define.cs` – 프로젝트 전역 enum/상수  
  - `Util.cs` – 계층 탐색, 컴포넌트 보장 유틸리티  
  - `Extension.cs` – GameObject 확장 메서드
    
### Item Structure
![Image](https://github.com/user-attachments/assets/8e673abe-ea12-49bf-bc2b-83854262cff1)
- **Data Layer**: `ItemData.asset` (ScriptableObject)로 공통 필드(id, name, icon 등)를 정의하고, `FeatherData.asset`, `WingData.asset` 등 개별 아이템 데이터를 에셋으로 분리  
- **Logic Layer**: `ItemData.cs`와 `IItemUse` 인터페이스를 통해 `Use()` 메서드를 추상화하고, `FeatherUse.cs`·`WingUse.cs`·`LampUse.cs`·`FlagUse.cs`에서 각 아이템 사용 효과 구현  
- **Management**: `ItemManager.cs`가 에셋 로드, 수량·쿨타임·사용 상태를 종합 관리  
- **UI**: `UI_Inventory.cs`가 슬롯별 아이콘·개수·쿨타임을 화면에 표시하여 플레이어 인벤토리를 시각화

[아이템 다이어그램](https://github.com/heejune1209/Just-climb-/blob/main/%EC%95%84%EC%9D%B4%ED%85%9C%20%EC%8B%9C%ED%80%80%EC%8A%A4%20%EB%8B%A4%EC%9D%B4%EC%96%B4%EA%B7%B8%EB%9E%A8.md)

### Obstacle Structure 
![image](https://github.com/user-attachments/assets/24ebe925-cbaf-4006-8240-513eebafee46)
- **Core Interface & Base**  
  `IObstacle.cs`(동작 계약), `ObstacleBase.cs`(Activate/Deactivate 공통 로직), `ObstacleTrigger.cs`(충돌 감지 → IObstacle 호출)로 모든 장애물의 기본 뼈대를 정의.

- **Obstacle Definitions**  
  `ObstacleData.asset`(공통 속성)과 `DropperData.asset`, `RollerData.asset`, `CannonData.asset` 같은 ScriptableObject에 개별 장애물 파라미터를 저장.

- **Spawner Components**  
  `RockDropper.cs`, `RollingSpawner.cs`, `CannonShooter.cs`가 정의된 Data Asset을 읽어 실제 장애물을 씬에 스폰하는 역할.

- **Obstacle Effects**  
  `KnockbackZone.cs`, `JumpPad.cs`, `DeathZone.cs`, `MaterialChanger.cs` 등으로 장애물에 닿았을 때 발생할 충돌 반응이나 특수 효과를 구현.

- **Pooling Support**  
  `PoolableObstacle.cs`는 Object Pooling 기능을 제공하며, `ObstacleBase`를 상속해 장애물 인스턴스를 재사용.  

## 주요 기여

### ✅ UI/UX 시스템 제작
- `UI_Scene`, `UI_Popup` 구조 설계 및 자동화 슬롯 생성 툴 제작
- 이벤트 기반 구조로 UI 갱신을 분리하여 유지보수성과 확장성 강화

### ✅ 아이템 시스템 설계 및 구현
- ScriptableObject + 인터페이스 기반 구조로 설계
- **신규 아이템 추가 시 코드 수정 없이 에셋 등록만으로 반영 가능**
- 쿨타임, 수량, UI 반영을 모두 `ItemManager`에서 일괄 처리

### ✅ 장애물 시스템 모듈화
- 장애물 **트리거 / 스폰 / 효과**를 명확히 분리하여 구조화
- 스폰 주기 및 파라미터를 **ScriptableObject**로 설정 가능하도록 유연하게 설계
- **풀링(Pooling)** 적용으로 실시간 낙석 및 발사 성능 최적화

### ✅ 클라이밍 시스템 분석 및 수정
- 기존 FSM 흐름 분석 후 **벽면 인식 로직 및 이동 제약 조건** 수정
- **경사면 처리 누락** 문제를 직접 해결하여 자연스러운 클라이밍 구현
  
### ✅ 캐릭터 능력치 밸런싱

## 🔧 **향후 개선 계획**
- ⏱ 스테이지별 클리어 타임 기반 랭킹 시스템 도입
  - 서버(API)에서 클리어 기록 등록 및 랭킹 데이터 처리  
  - Redis 기반 실시간 랭킹 정렬  
  - 클리어 기록 등록 및 내 랭킹 조회 기능 구현  
  - UI에서 상위 랭커와 본인 순위 확인 가능  
  
- 🎮 **클라이밍 조작 보정 및 최적화**
  - 현재 클라이밍 방식은 조작키와 플레이어가 바라보고 있는 방향으로 가장 가까운 홀드로 이동을 하지만,
  - 이 부분이 클라이밍을 할때 플레이어가 원하는 홀드로 이동이 안될수 있음을 파악했음.
  - 입력 방향과 카메라 시선 가중치 기반으로 플레이어가 의도한 홀드를 우선 선택하도록 개선

- 💾 **데이터 관리 개선**  
  - PlayerPrefs → `SaveDataManager` + JSON 파일 기반 직렬화로 전환  
  - 재화(coins), 아이템 수량 등 동적 데이터는 `save.json`에 저장/로드  
  - 아이템 정의, 가격 등 정적 데이터는 `ScriptableObject` 또는 별도 JSON 설정 파일로 관리  
  - 데이터 구조 변경 시 마이그레이션, 백업 지원 로직 추가

- 🎭 **캐릭터 선택 기능 설계 개선**
  - ScriptableObject 기반 데이터  
  - UI 패널에서 아이콘 클릭으로 선택  
  - 매니저 스크립트로 미리보기,확정 처리  
  - JSON/서버 연동으로 선택 정보 저장  

## 기술 스택 및 개발 환경
C#, Unity3D, Visual Studio 2022

## 관련 링크
### 시연 동영상
https://youtu.be/HkNdxTHhVaw?si=IY2KK07vDpRfTPzP
