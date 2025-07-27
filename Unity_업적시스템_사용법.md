# Unity 업적 시스템 사용법

## 📋 개요

Steam 파트너에서 등록한 업적들을 Unity에서 자동으로 관리하는 시스템입니다.

### 🎯 등록된 업적 목록 (총 18개)

#### Stage Achievements (10개)
- `Novice_Climber` - 초보 등반가
- `Intermediate_Climber` - 중급 등반가  
- `Advanced_Climber` - 고급 등반가
- `CHAPTER_1_MASTER` - 첫 산 정복!
- `CHAPTER_2_MASTER` - 두번째 산 정복!
- `CHAPTER_3_MASTER` - 세번째 산 정복!
- `CHAPTER_4_MASTER` - 네번째 산 정복!
- `CHAPTER_5_MASTER` - 5번째 산 정복!
- `Mountain_god` - 산신
- `Speed_Climber` - 야빠른 평지러님
- `PERFECTIONIST` - 완벽주의자
- `FLAWLESS_Climb` - 완벽한 등반
- `UNTOUCHABLE` - 언터처블
- `Zombie` - 좀비

#### Character Achievements (3개)
- `Unlock_Braden` - Braden 해금
- `Unlock_Lina` - Lina 해금
- `Unlock_Elliott` - Elliott 해금

#### Item Achievements (5개)
- `FIRST_PURCHASE` - 첫 구매
- `COLLECTOR` - 수집가
- `Shop_VIP` - VIP 고객
- `NATURAL_CLIMBER` - 맨손 등반가
- `TOOL_MASTER` - 도구 마스터

## 🚀 설정 방법

### 1. Zenject 바인딩 추가

```csharp
// ProjectInstaller.cs에 추가
public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // 기존 바인딩들...
        
        Container.Bind<AchievementManager>()
            .FromComponentInHierarchy()
            .AsSingle();
    }
}
```

### 2. 씬에 컴포넌트 추가

1. **GameManager가 있는 씬**에 빈 GameObject 생성
2. `AchievementManager` 컴포넌트 추가
3. `AchievementInitializer` 컴포넌트 추가

## 🎮 게임 코드 연동

### 1. 스테이지 관련 이벤트

```csharp
// GameManager.cs 또는 StageController.cs에서
public class GameManager : MonoBehaviour
{
    private float stageStartTime;
    private int currentStageIndex;
    
    public void OnStageStart(int stageIndex)
    {
        currentStageIndex = stageIndex;
        stageStartTime = Time.time;
        
        // 업적 시스템에 스테이지 시작 알림
        AchievementIntegration.OnStageStart();
    }
    
    public void OnStageComplete(int gemsCollected, int totalGems)
    {
        float clearTime = Time.time - stageStartTime;
        int deathCount = GetCurrentDeathCount(); // 현재 스테이지 사망 횟수
        
        // 업적 시스템에 스테이지 클리어 알림
        AchievementIntegration.OnStageCleared(
            currentStageIndex, 
            clearTime, 
            deathCount, 
            gemsCollected, 
            totalGems
        );
        
        Debug.Log($"Stage {currentStageIndex} completed!");
    }
}
```

### 2. 플레이어 사망 이벤트

```csharp
// Player.cs 또는 PlayerController.cs에서
public class Player : MonoBehaviour
{
    public void Die()
    {
        // 기존 사망 처리 로직...
        
        // 업적 시스템에 사망 알림
        AchievementIntegration.OnPlayerDeath();
        
        Debug.Log("Player died!");
    }
}
```

### 3. 캐릭터 해제 이벤트

```csharp
// CharacterManager.cs 또는 Shop.cs에서
public class CharacterManager : MonoBehaviour
{
    public void UnlockCharacter(string characterName)
    {
        // 기존 캐릭터 해제 로직...
        
        // 업적 시스템에 캐릭터 해제 알림
        AchievementIntegration.OnCharacterUnlocked(characterName);
        
        Debug.Log($"Character {characterName} unlocked!");
    }
}
```

### 4. 아이템 관련 이벤트

```csharp
// Shop.cs에서
public class Shop : MonoBehaviour
{
    public void PurchaseItem(string itemType)
    {
        // 기존 아이템 구매 로직...
        
        // 업적 시스템에 아이템 구매 알림
        AchievementIntegration.OnItemPurchased(itemType);
        
        Debug.Log($"Item purchased: {itemType}");
    }
}

// Inventory.cs에서
public class Inventory : MonoBehaviour
{
    public void UseItem(string itemType)
    {
        // 기존 아이템 사용 로직...
        
        // 업적 시스템에 아이템 사용 알림
        AchievementIntegration.OnItemUsed(itemType);
        
        Debug.Log($"Item used: {itemType}");
    }
}
```

## 🏆 업적 달성 조건

### 자동 달성 업적

다음 업적들은 조건을 만족하면 자동으로 달성됩니다:

#### 스테이지 클리어 업적
- **초보 등반가**: 1번째 스테이지 클리어
- **중급 등반가**: 5번째 스테이지 클리어  
- **고급 등반가**: 10번째 스테이지 클리어
- **챕터 마스터들**: 각 챕터 마지막 스테이지 클리어
- **산신**: 모든 챕터 완주

#### 특수 조건 업적
- **완벽주의자**: 무사망으로 스테이지 클리어
- **완벽한 등반**: 5개 스테이지를 완벽하게 클리어 (무사망 + 모든 젬)
- **언터처블**: Chapter 1의 모든 스테이지를 완벽하게 클리어
- **야빠른 평지러님**: 30초 이내에 스테이지 클리어
- **좀비**: 한 스테이지에서 100번 이상 사망 후 클리어
- **맨손 등반가**: 아이템 사용 없이 스테이지 클리어

#### 수집 업적
- **첫 구매**: 첫 번째 아이템 구매
- **VIP 고객**: 10개 이상 아이템 구매
- **수집가**: 20개 이상 아이템 구매
- **도구 마스터**: 10종류 이상 아이템 사용

#### 캐릭터 업적
- **Braden/Lina/Elliott 해금**: 각각 해당 캐릭터 해제

## 🔧 디버그 및 테스트

### 1. 업적 테스트

```csharp
// 인스펙터에서 실행 가능한 테스트 메서드들
[ContextMenu("Test Unlock Achievement")]
public void TestUnlock()
{
    AchievementIntegration.TestUnlockAchievement();
}

[ContextMenu("Print Progress")]
public void PrintProgress()
{
    AchievementIntegration.PrintCurrentProgress();
}
```

### 2. 콘솔 명령어

```csharp
// 개발자 콘솔에서 사용 가능
AchievementIntegration.PrintCurrentProgress(); // 현재 진행률 출력
AchievementIntegration.TestUnlockAchievement(); // 테스트 업적 해제
```

### 3. Steam 클라이언트에서 확인

1. Steam 클라이언트 실행
2. 라이브러리에서 게임 우클릭
3. **업적 보기** 클릭
4. 달성된 업적과 진행률 확인

## 📋 구현 체크리스트

### 필수 설정
- [ ] Steam 파트너에서 18개 업적 등록 완료
- [ ] AchievementManager 씬에 추가
- [ ] AchievementInitializer 컴포넌트 추가
- [ ] Zenject 바인딩 설정

### 게임 연동
- [ ] 스테이지 시작/클리어 이벤트 연동
- [ ] 플레이어 사망 이벤트 연동
- [ ] 캐릭터 해제 이벤트 연동
- [ ] 아이템 구매/사용 이벤트 연동

### 테스트
- [ ] Steam 클라이언트에서 업적 확인
- [ ] 각 업적 달성 조건 테스트
- [ ] 진행률 저장/로드 테스트
- [ ] 게임 재시작 후 진행률 유지 확인

## 🚨 주의사항

1. **Steam 클라이언트 필수**: Steam이 실행되지 않으면 업적이 작동하지 않음
2. **업적 ID 일치**: Steam 파트너의 API Name과 코드의 상수가 정확히 일치해야 함
3. **중복 해제 방지**: 이미 달성한 업적은 다시 해제되지 않음
4. **진행률 저장**: PlayerPrefs에 진행률이 저장되므로 삭제 시 초기화됨
5. **네트워크 필요**: 업적 동기화를 위해 인터넷 연결 필요

## 💡 확장 가능성

### 추가 업적 구현

새로운 업적을 추가하려면:

1. **Steam 파트너**에서 업적 등록
2. **AchievementIDs**에 상수 추가
3. **AchievementManager**에 체크 로직 추가
4. **게임 코드**에서 해당 이벤트 호출

### UI 연동

기존 `UI_Achievement`와 연동하여:
- 실시간 진행률 표시
- 업적 달성 알림
- 보상 수령 시스템

이제 완전한 Steam 업적 시스템이 구축되었습니다! 🎉 