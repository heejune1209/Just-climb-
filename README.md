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
  `PoolableObstacle.cs`는 Object Pooling 기능을 제공하며, `ObstacleBase`를 상속해 장애물 인스턴스를 효율적으로 재사용.  

## 주요 역할
- UI,UX 시스템 제작
- 캐릭터 클라이밍 시스템 분석 및 수정 
- 캐릭터 능력치 밸런싱
- ScriptableObject 기반 ItemData와 IItemUse 인터페이스를 사용해 아이템 확장성을 확보하고, 쿨타임/사용 로직을 ItemManager에 통합하여 구조화
- ObstacleBase와 ObstacleTrigger를 중심으로 장애물 감지,스폰 구조를 구축, RockDropper 등 장애물은 개별 SO 파라미터로 제어 가능

## 기술 스택 및 개발 환경
C#, Unity3D, Visual Studio 2022

## 관련 링크
### 시연 동영상
https://youtu.be/HkNdxTHhVaw?si=IY2KK07vDpRfTPzP
