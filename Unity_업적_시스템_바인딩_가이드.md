# Unity 에디터 업적 시스템 바인딩 가이드 (동적 생성 버전)

## 📋 개요
Just Climb의 Steam 업적 시스템을 Unity 에디터에서 설정하는 방법을 안내합니다.

**실제 업적 개수:**
- **Stage Achievements**: 14개
- **Character Achievements**: 3개  
- **Item Achievements**: 5개
- **총 22개**

**✨ 새로운 특징: 동적 버튼 생성**
- 프리팹 버튼 1개만 설정하면 스크립트에서 자동으로 필요한 개수만큼 생성
- 복잡한 바인딩 작업 없이 간단한 설정만으로 완성

---

## 🎯 1. UI_Achievement 팝업 설정

### 📍 위치
`Assets/Scenes/` → 업적 UI가 포함된 씬

### 🔧 바인딩 설정

#### A. UI_Achievement 컴포넌트
```
GameObject: AchievementPopup
├── Component: UI_Achievement
├── [Inject] ISoundManager (자동 주입)
├── [Inject] IAchievementManager (자동 주입)  
└── [Inject] ICurrencyManager (자동 주입)
```

#### B. 버튼 바인딩 (Buttons enum) - **5개만**
```
CloseButton        → "CloseButton" GameObject의 Button 컴포넌트
RewardButton       → "RewardButton" GameObject의 Button 컴포넌트 (공통 표시 영역)
StageTabButton     → "StageTabButton" GameObject의 Button 컴포넌트
CharacterTabButton → "CharacterTabButton" GameObject의 Button 컴포넌트
ItemTabButton      → "ItemTabButton" GameObject의 Button 컴포넌트
```

#### C. 텍스트 바인딩 (Texts enum) - **4개만**
```
CategoryText → "CategoryText" GameObject의 TextMeshProUGUI 컴포넌트
TitleText    → "TitleText" GameObject의 TextMeshProUGUI 컴포넌트 (공통)
DescText     → "DescText" GameObject의 TextMeshProUGUI 컴포넌트 (공통)
RewardText   → "RewardText" GameObject의 TextMeshProUGUI 컴포넌트 (공통)
```

#### D. 게임오브젝트 바인딩 (GameObjects enum) - **1개만**
```
ContentRoot → "ContentRoot" GameObject (업적 버튼들이 생성될 부모)
```

#### E. 동적 버튼 생성 설정 (Inspector) - **매우 간단!**
```
동적 버튼 생성 설정:

Achievement Button Prefab → 업적 버튼 프리팹 1개만 드래그
Stage Button Count: 14
Character Button Count: 3
Item Button Count: 5
```

---

## 🏗️ 2. UI 구조 설계

### 🎨 **권장 UI 계층 구조**
```
AchievementPopup
├── Header
│   ├── CategoryText (현재 카테고리 표시)
│   └── CloseButton
├── TabButtons
│   ├── StageTabButton
│   ├── CharacterTabButton
│   └── ItemTabButton
├── ContentRoot (버튼들이 동적으로 생성될 부모)
│   └── (스크립트에서 자동 생성되는 업적 버튼들)
└── DetailArea (공통 표시 영역)
    ├── TitleText
    ├── DescText
    ├── RewardText
    └── RewardButton
```

### 🔧 **프리팹 버튼 설정**
```
AchievementButtonPrefab:
├── Button 컴포넌트
├── TextMeshProUGUI (버튼 텍스트용)
└── 기타 UI 요소들 (배경, 아이콘 등)
```

---

## 🏆 3. 업적 데이터 설정

### 📊 Stage Achievements (Inspector) - **14개**
```
Stage Achievements (List<AchievementData>) - Size: 14

[0] 초보 등반가
├── Title: "초보 등반가"
├── Description: "첫 번째 스테이지를 클리어"
├── Reward: "50 Gems"
└── Steam Achievement Id: "Novice_Climber"

[1] 중급 등반가
├── Title: "중급 등반가"
├── Description: "5번째 스테이지를 클리어"
├── Reward: "100 Gems"
└── Steam Achievement Id: "Intermediate_Climber"

[2] 고급 등반가
├── Title: "고급 등반가"
├── Description: "10번째 스테이지를 클리어"
├── Reward: "200 Gems"
└── Steam Achievement Id: "Advanced_Climber"

[3] 첫 산 정복!
├── Title: "첫 산 정복!"
├── Description: "Chapter 1 클리어"
├── Reward: "75 Gems"
└── Steam Achievement Id: "CHAPTER_1_MASTER"

[4] 두번째 산 정복!
├── Title: "두번째 산 정복!"
├── Description: "Chapter 2 클리어"
├── Reward: "150 Gems"
└── Steam Achievement Id: "CHAPTER_2_MASTER"

[5] 세번째 산 정복!
├── Title: "세번째 산 정복!"
├── Description: "Chapter 3 클리어"
├── Reward: "200 Gems"
└── Steam Achievement Id: "CHAPTER_3_MASTER"

[6] 네번째 산 정복!
├── Title: "네번째 산 정복!"
├── Description: "Chapter 4 클리어"
├── Reward: "250 Gems"
└── Steam Achievement Id: "CHAPTER_4_MASTER"

[7] 5번째 산 정복!
├── Title: "5번째 산 정복!"
├── Description: "Chapter 5 클리어"
├── Reward: "300 Gems"
└── Steam Achievement Id: "CHAPTER_5_MASTER"

[8] 산신
├── Title: "산신"
├── Description: "모든 챕터 클리어"
├── Reward: "1000 Gems"
└── Steam Achievement Id: "Mountain_god"

[9] 야빠른 평지러님
├── Title: "야빠른 평지러님"
├── Description: "임의의 스테이지를 30초 안에 클리어"
├── Reward: "100 Gems"
└── Steam Achievement Id: "Speed_Climber"

[10] 완벽주의자
├── Title: "완벽주의자"
├── Description: "임의의 스테이지를 한번도 죽지 않고 클리어"
├── Reward: "150 Gems"
└── Steam Achievement Id: "PERFECTIONIST"

[11] 완벽한 등반
├── Title: "완벽한 등반"
├── Description: "5개의 스테이지를 클리어 완벽 점부 보석 3개를 클리어"
├── Reward: "400 Gems"
└── Steam Achievement Id: "FLAWLESS_Climb"

[12] 언터처블
├── Title: "언터처블"
├── Description: "Chapter1의 모든 스테이지를 전부 보석 3개를 클리어"
├── Reward: "500 Gems"
└── Steam Achievement Id: "UNTOUCHABLE"

[13] 좀비
├── Title: "좀비"
├── Description: "임의의 스테이지를 100번이상 죽고 클리어"
├── Reward: "200 Gems"
└── Steam Achievement Id: "Zombie"
```

### 👥 Character Achievements (Inspector) - **3개**
```
Character Achievements (List<AchievementData>) - Size: 3

[0] Braden 해금
├── Title: "Braden 해금"
├── Description: "Braden 해금"
├── Reward: "100 Gems"
└── Steam Achievement Id: "Unlock_Braden"

[1] Lina 해금
├── Title: "Lina 해금"
├── Description: "Lina 해금"
├── Reward: "100 Gems"
└── Steam Achievement Id: "Unlock_Lina"

[2] Elliott 해금
├── Title: "Elliott 해금"
├── Description: "Elliott 해금"
├── Reward: "100 Gems"
└── Steam Achievement Id: "Unlock_Elliott"
```

### 🛍️ Item Achievements (Inspector) - **5개**
```
Item Achievements (List<AchievementData>) - Size: 5

[0] 첫 구매
├── Title: "첫 구매"
├── Description: "아이템을 처음으로 구매"
├── Reward: "25 Gems"
└── Steam Achievement Id: "FIRST_PURCHASE"

[1] 수집가
├── Title: "수집가"
├── Description: "모든 종류의 아이템을 구매"
├── Reward: "200 Gems"
└── Steam Achievement Id: "COLLECTOR"

[2] VIP 고객
├── Title: "VIP 고객"
├── Description: "아이템을 10개 이상 구매"
├── Reward: "300 Gems"
└── Steam Achievement Id: "Shop_VIP"

[3] 맨손 등반가
├── Title: "맨손 등반가"
├── Description: "완벽한 아이템 사용없이 임의의 스테이지 클리어"
├── Reward: "150 Gems"
└── Steam Achievement Id: "NATURAL_CLIMBER"

[4] 도구 마스터
├── Title: "도구 마스터"
├── Description: "모든 종류의 아이템을 사용"
├── Reward: "200 Gems"
└── Steam Achievement Id: "TOOL_MASTER"
```

---

## 🔧 4. AchievementManager 설정

### 📍 위치
`Assets/Scripts/Managers/AchievementManager.cs`

### ⚙️ 컴포넌트 설정
```
GameObject: AchievementManager (자동 생성됨)
├── Component: AchievementManager
├── [Inject] GameManager (자동 주입)
└── [Inject] IDataManager (자동 주입)
```

### 🧪 테스트 메서드 (Context Menu)
```
우클릭 → Context Menu:
├── "Test Unlock Achievement" → 테스트용 업적 해제
├── "Reset All Achievements" → 모든 업적 리셋
└── "Print Progress" → 현재 진행률 출력
```

---

## 🔗 5. AchievementInitializer 설정

### 📍 위치
메인 씬의 GameManager나 별도 GameObject에 추가

### ⚙️ 컴포넌트 설정
```
GameObject: GameManager (또는 AchievementInitializer)
├── Component: AchievementInitializer
└── [Inject] IAchievementManager (자동 주입)
```

---

## 🎮 6. 게임 이벤트 연동

### 📍 연동할 스크립트들

#### A. GameManager
```csharp
// 스테이지 시작 시
AchievementIntegration.OnStageStart();

// 스테이지 클리어 시
AchievementIntegration.OnStageCleared(stageIndex, clearTime, deathCount, gemsCollected, totalGems);
```

#### B. Player 스크립트
```csharp
// 플레이어 사망 시
AchievementIntegration.OnPlayerDeath();
```

#### C. CharacterManager/Shop
```csharp
// 캐릭터 해제 시
AchievementIntegration.OnCharacterUnlocked("Braden");
```

#### D. ItemManager/Shop
```csharp
// 아이템 구매 시
AchievementIntegration.OnItemPurchased("HealthPotion");

// 아이템 사용 시
AchievementIntegration.OnItemUsed("HealthPotion");
```

---

## 📋 7. 설정 체크리스트

### ✅ UI 설정 (매우 간단!)
- [ ] UI_Achievement 컴포넌트 추가
- [ ] 버튼 바인딩 완료 (**5개만**)
- [ ] 텍스트 바인딩 완료 (**4개만**)
- [ ] ContentRoot 바인딩 완료 (**1개만**)
- [ ] Achievement Button Prefab 설정 (**1개만**)
- [ ] 카테고리별 버튼 개수 설정 (14, 3, 5)

### ✅ 업적 데이터 설정
- [ ] Stage Achievements 설정 (**14개**)
- [ ] Character Achievements 설정 (**3개**)
- [ ] Item Achievements 설정 (**5개**)
- [ ] 모든 Steam Achievement ID 입력

### ✅ 매니저 설정
- [ ] AchievementManager 자동 생성 확인
- [ ] AchievementInitializer 추가
- [ ] Zenject 의존성 주입 확인

### ✅ 이벤트 연동
- [ ] GameManager 이벤트 연동
- [ ] Player 사망 이벤트 연동
- [ ] 캐릭터 해제 이벤트 연동
- [ ] 아이템 구매/사용 이벤트 연동

---

## 🚨 주의사항

1. **Steam Achievement ID**: Steam 파트너 대시보드와 정확히 일치해야 함
2. **의존성 주입**: Zenject 시스템이 올바르게 설정되어야 함
3. **이벤트 호출**: 게임 로직에서 적절한 시점에 이벤트 호출 필요
4. **테스트**: Steam 테스트 환경에서 업적 해제 확인 필요
5. **프리팹 설정**: Achievement Button Prefab이 올바르게 설정되어야 함

---

## 🔍 디버깅 팁

### Console 로그 확인
```
"Achievement Integration Initialized" → 초기화 성공
"Achievement: Stage Started" → 스테이지 시작 이벤트
"Achievement: Stage X cleared" → 스테이지 클리어 이벤트
"Achievement Unlocked: ACHIEVEMENT_ID" → 업적 해제 성공
"Achievement Button Prefab이 설정되지 않았습니다!" → 프리팹 미설정 오류
```

### Context Menu 활용
- **Print Progress**: 현재 진행률 확인
- **Test Unlock Achievement**: 테스트용 업적 해제
- **Reset All Achievements**: 테스트 후 초기화

---

## 💡 동적 생성 방식의 장점

**🚀 극도로 간단한 설정:**
- 프리팹 버튼 1개만 만들면 끝
- 복잡한 개별 바인딩 작업 불필요
- 카테고리별 개수만 숫자로 설정

**🎨 자동 스타일링:**
- 업적 상태에 따른 자동 색상 변경
- 선택된 버튼 자동 하이라이트
- 보상 수령 상태 시각적 표시

**🔧 확장성:**
- 새 카테고리 추가 시 개수만 변경하면 됨
- 프리팹 스타일 변경으로 모든 버튼 일괄 적용
- 런타임에서 동적 개수 조정 가능

**⚡ 성능:**
- 필요한 버튼만 생성하여 메모리 효율성
- 카테고리 전환 시 기존 버튼 제거 후 새로 생성
- 불필요한 UI 요소 최소화

이제 Unity 에디터에서 위 가이드에 따라 설정하면 매우 간단하게 Steam 업적 시스템이 완성됩니다! 🎉 