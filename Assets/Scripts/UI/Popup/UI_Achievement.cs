using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Zenject;
using Steamworks;
using System;
using JustClimb.Data;

// 팝업 형태로 업적 UI를 관리하는 스크립트
public class UI_Achievement : UI_Popup
{
    // DI 주입받을 매니저들
    [Inject] private ISoundManager _soundManager;
    [Inject] private IAchievementManager _achievementManager;
    [Inject] private ICurrencyManager _currencyManager;
    [Inject] private IResourceManager _resourceManager;
    [Inject] private IDataManager _dataManager;

    [Header("Steam 테스트 설정")]
    [Tooltip("에디터에서도 실제 Steam API를 사용할지 여부 (테스트용)")]
    [SerializeField] private bool _useRealSteamInEditor = false;

    // 바인딩할 버튼
    enum Buttons
    {
        CloseButton,        // 팝업 닫기
        RewardButton,       // 보상 받기 (공통)
        StageTabButton,     // 스테이지 카테고리
        CharacterTabButton, // 캐릭터 카테고리
        ItemTabButton,      // 아이템 카테고리
    }

    // 바인딩할 텍스트 (공통 표시 영역)
    enum Texts
    {
        CategoryText,   // 현재 카테고리 제목
        TitleText,      // 업적 타이틀 (공통)
        DescText,       // 업적 설명 (공통)
        RewardText,     // 보상 텍스트 (공통)
        ProgressText,   // 진행률 텍스트 (새로 추가)
    }

    // 바인딩할 패널
    enum GameObjects
    {
        ContentRoot     // 업적 버튼들이 생성될 부모
    }

    // 바인딩할 이미지 (보상 상태 표시용)
    enum Images
    {
        LockImage,      // 자물쇠 이미지
    }



    [Header("동적 버튼 생성 설정")]
    [Tooltip("각 카테고리별 버튼 개수 (EntryButton0 프리팹을 자동으로 사용)")]
    [SerializeField] int stageButtonCount = 14;
    [SerializeField] int characterButtonCount = 3;
    [SerializeField] int itemButtonCount = 5;

    [Header("업적 데이터")]
    public List<AchievementData> stageAchievements = new List<AchievementData>();
    public List<AchievementData> characterAchievements = new List<AchievementData>();
    public List<AchievementData> itemAchievements = new List<AchievementData>();

    [Header("카테고리 제목 설정")]
    [SerializeField] string stageCategoryTitle = "스테이지 업적";
    [SerializeField] string characterCategoryTitle = "캐릭터 업적";
    [SerializeField] string itemCategoryTitle = "아이템 업적";

    // 카테고리 정의
    enum Category { Stage, Character, Item }
    Category _currentCategory = Category.Stage;
    int _currentIndex = 0;

    // 바인딩 참조 (공통 표시 영역)
    TMP_Text _categoryTitle;
    TMP_Text _title;
    TMP_Text _desc;
    TMP_Text _reward;
    TMP_Text _progressText;      // 진행률 텍스트
    Button _rewardBtn;
    Transform _contentRoot;
    Image _lockImage;            // 자물쇠 이미지

    // 동적으로 생성된 버튼들
    List<Button> _generatedButtons = new List<Button>();

    private void Start()
    {
        Init();
        
        // Steam 테스트 설정을 AchievementManager에 전달
        ApplySteamSettings();
        
        // 공통 표시 영역 바인딩
        _categoryTitle = GetText((int)Texts.CategoryText);
        _title = GetText((int)Texts.TitleText);
        _desc = GetText((int)Texts.DescText);
        _reward = GetText((int)Texts.RewardText);
        _progressText = GetText((int)Texts.ProgressText);        // 진행률 텍스트
        _rewardBtn = GetButton((int)Buttons.RewardButton);
        _contentRoot = GetGameObject((int)GameObjects.ContentRoot).transform;
        _lockImage = GetImage((int)Images.LockImage);            // 자물쇠 이미지

        // 이벤트 설정
        SetupEvents();
        
        // 초기 표시
        SwitchCategory(Category.Stage);
    }

    /// <summary>
    /// Steam 테스트 설정을 AchievementManager에 적용
    /// </summary>
    private void ApplySteamSettings()
    {
        // AchievementManager를 MonoBehaviour로 찾아서 설정 적용
        var achievementManagerMB = FindObjectOfType<AchievementManager>();
        if (achievementManagerMB != null)
        {
            achievementManagerMB.SetUseRealSteamInEditor(_useRealSteamInEditor);
            Debug.Log($"[UI_Achievement] Steam 테스트 설정 적용: useRealSteamInEditor = {_useRealSteamInEditor}");
        }
        else
        {
            Debug.LogWarning("[UI_Achievement] AchievementManager MonoBehaviour를 찾을 수 없습니다.");
        }
    }

    public override void Init()
    {
        base.Init();
        // 자동 바인딩
        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));
        Bind<Image>(typeof(Images));         // 이미지 바인딩
        
        // 닫기 버튼
        GetButton((int)Buttons.CloseButton).gameObject.BindEvent(_ =>
        {
            _soundManager.PlaySFX(0);
            ClosePopupUI();
        });
    }

    void SetupEvents()
    {
        // 보상 받기 버튼
        _rewardBtn.onClick.AddListener(() =>
        {
            _soundManager.PlaySFX(0);
            OnRewardPressed();
        });

        // 카테고리 탭 버튼들
        GetButton((int)Buttons.StageTabButton).onClick.AddListener(() =>
        {
            _soundManager.PlaySFX(0);
            SwitchCategory(Category.Stage);
        });
        
        GetButton((int)Buttons.CharacterTabButton).onClick.AddListener(() =>
        {
            _soundManager.PlaySFX(0);
            SwitchCategory(Category.Character);
        });
        
        GetButton((int)Buttons.ItemTabButton).onClick.AddListener(() =>
        {
            _soundManager.PlaySFX(0);
            SwitchCategory(Category.Item);
        });
    }

    void SwitchCategory(Category category)
    {
        _currentCategory = category;
        _currentIndex = 0;
        
        CreateCategoryButtons();
        RefreshDetailDisplay();
    }

    void CreateCategoryButtons()
    {
        // 기존 생성된 버튼들 제거
        ClearGeneratedButtons();
        
        var achievements = GetCurrentAchievements();
        int buttonCount = GetCurrentButtonCount();
        
        // 필요한 개수만큼 버튼 생성
        for (int i = 0; i < buttonCount && i < achievements.Count; i++)
        {
            CreateAchievementButton(i, achievements[i]);
        }
    }

    void ClearGeneratedButtons()
    {
        // 기존 버튼들 풀로 반환
        foreach (var button in _generatedButtons)
        {
            if (button != null)
            {
                _resourceManager.Destroy(button.gameObject);
            }
        }
        _generatedButtons.Clear();
    }

    void CreateAchievementButton(int index, AchievementData achievement)
    {
        // ResourceManager를 통한 풀링 생성
        GameObject buttonGO = _resourceManager.Instantiate("Prefabs/UI/Scene/EntryButton0", _contentRoot);
        if (buttonGO == null)
        {
            Debug.LogError("EntryButton0 프리팹을 찾을 수 없습니다! Resources/Prefabs/UI/Scene/EntryButton0.prefab을 확인하세요.");
            return;
        }

        Button newButton = buttonGO.GetComponent<Button>();
        if (newButton == null)
        {
            Debug.LogError("생성된 버튼에 Button 컴포넌트가 없습니다!");
            _resourceManager.Destroy(buttonGO);
            return;
        }

        _generatedButtons.Add(newButton);
        
        // 버튼 텍스트 설정
        var label = newButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = achievement.title;
        }
        
        // 업적 상태에 따른 버튼 스타일 설정
        UpdateButtonAppearance(newButton, achievement);
        
        // 클릭 이벤트 설정
        int buttonIndex = index; // 클로저 캡처용
        newButton.onClick.RemoveAllListeners();
        newButton.onClick.AddListener(() =>
        {
            _soundManager.PlaySFX(0);
            OnAchievementButtonClicked(buttonIndex);
        });
        
        newButton.gameObject.SetActive(true);
    }

    void UpdateButtonAppearance(Button button, AchievementData achievement)
    {
        if (_achievementManager == null) return;
        
        bool isUnlocked = _achievementManager.IsAchievementUnlocked(achievement.steamAchievementId);
        bool isRewardClaimed = _achievementManager.IsRewardClaimed(achievement.steamAchievementId);
        
        // 버튼 색상 변경 (예시)
        var colors = button.colors;
        if (isUnlocked && isRewardClaimed)
        {
            // 완료된 업적 (회색)
            colors.normalColor = Color.gray;
        }
        else if (isUnlocked)
        {
            // 달성했지만 보상 미수령 (초록색)
            colors.normalColor = Color.green;
        }
        else
        {
            // 미달성 업적 (기본색)
            colors.normalColor = Color.white;
        }
        button.colors = colors;
    }

    void OnAchievementButtonClicked(int index)
    {
        _currentIndex = index;
        RefreshDetailDisplay();
    }

    int GetCurrentButtonCount()
    {
        switch (_currentCategory)
        {
            case Category.Stage: return stageButtonCount;
            case Category.Character: return characterButtonCount;
            case Category.Item: return itemButtonCount;
            default: return 0;
        }
    }

    List<AchievementData> GetCurrentAchievements()
    {
        switch (_currentCategory)
        {
            case Category.Stage: return stageAchievements;
            case Category.Character: return characterAchievements;
            case Category.Item: return itemAchievements;
            default: return stageAchievements;
        }
    }

    void RefreshDetailDisplay()
    {
        var achievements = GetCurrentAchievements();
        if (achievements.Count == 0) return;
        
        _currentIndex = Mathf.Clamp(_currentIndex, 0, achievements.Count - 1);
        var data = achievements[_currentIndex];
        
        // 공통 표시 영역 업데이트
        _categoryTitle.text = GetCategoryDisplayName(_currentCategory);
        _title.text = data.title;
        _desc.text = data.description;
        _reward.text = data.reward;
        
        // 진행률 계산 및 표시
        UpdateProgressDisplay(data);
        
        // 보상 상태 시각화
        UpdateRewardVisuals(data);
        
        // 선택된 버튼 하이라이트 (선택사항)
        HighlightSelectedButton();
    }

    /// <summary>
    /// 진행률 계산 및 표시
    /// </summary>
    void UpdateProgressDisplay(AchievementData data)
    {
        if (_achievementManager == null || _dataManager?.Current?.achievementProgress == null)
        {
            _progressText.text = "Progress: Unknown";
            return;
        }
        
        var progress = _dataManager.Current.achievementProgress;
        float currentProgress = 0f;
        float maxProgress = 1f;
        string progressText = "";
        
        // 업적 ID에 따른 진행률 계산
        switch (data.steamAchievementId)
        {
            case AchievementIDs.NOVICE_CLIMBER:
                progressText = "Clear First Stage";
                currentProgress = progress.stagesCompleted >= 1 ? 1f : 0f;
                break;
                
            case AchievementIDs.INTERMEDIATE_CLIMBER:
                progressText = $"Stages Cleared: {Math.Min(progress.stagesCompleted, 5)}/5";
                currentProgress = Math.Min(progress.stagesCompleted, 5);
                maxProgress = 5f;
                break;
                
            case AchievementIDs.ADVANCED_CLIMBER:
                progressText = $"Stages Cleared: {Math.Min(progress.stagesCompleted, 10)}/10";
                currentProgress = Math.Min(progress.stagesCompleted, 10);
                maxProgress = 10f;
                break;
                
            case AchievementIDs.FLAWLESS_CLIMB:
                progressText = $"Perfect Clears: {Math.Min(progress.perfectClears, 5)}/5";
                currentProgress = Math.Min(progress.perfectClears, 5);
                maxProgress = 5f;
                break;
                
            case AchievementIDs.SPEED_CLIMBER:
                progressText = $"Speed Clears: {Math.Min(progress.speedClears, 1)}/1";
                currentProgress = Math.Min(progress.speedClears, 1);
                maxProgress = 1f;
                break;
                
            case AchievementIDs.FIRST_PURCHASE:
                progressText = $"Items Purchased: {Math.Min(progress.itemsPurchased, 1)}/1";
                currentProgress = Math.Min(progress.itemsPurchased, 1);
                maxProgress = 1f;
                break;
                
            case AchievementIDs.SHOP_VIP:
                progressText = $"Items Purchased: {Math.Min(progress.itemsPurchased, 10)}/10";
                currentProgress = Math.Min(progress.itemsPurchased, 10);
                maxProgress = 10f;
                break;
                
            case AchievementIDs.COLLECTOR:
                progressText = $"Items Purchased: {Math.Min(progress.itemsPurchased, 4)}/4";
                currentProgress = Math.Min(progress.itemsPurchased, 4);
                maxProgress = 4f;
                break;
                
            case AchievementIDs.TOOL_MASTER:
                progressText = $"Item Types Used: {Math.Min(progress.itemTypesUsed.Count, 4)}/4";
                currentProgress = Math.Min(progress.itemTypesUsed.Count, 4);
                maxProgress = 4f;
                break;
                
            case AchievementIDs.UNTOUCHABLE:
                progressText = $"Chapter 1 Perfect Clears: {Math.Min(progress.chapter1PerfectStages, 10)}/10";
                currentProgress = Math.Min(progress.chapter1PerfectStages, 10);
                maxProgress = 10f;
                break;
                
            default:
                progressText = "Special Conditions Required";
                currentProgress = _achievementManager.IsAchievementUnlocked(data.steamAchievementId) ? 1f : 0f;
                maxProgress = 1f;
                break;
        }
        
        // UI 업데이트
        _progressText.text = $"Progress: {progressText}";
        
        // Steam 업적 달성 여부 표시
        bool isUnlocked = _achievementManager.IsAchievementUnlocked(data.steamAchievementId);
        if (isUnlocked)
        {
            _progressText.text += "Achieved!";
        }
    }

    /// <summary>
    /// 보상 상태 시각화 (자물쇠와 보석 투명도)
    /// </summary>
    void UpdateRewardVisuals(AchievementData data)
    {
        if (_achievementManager == null)
        {
            // 기본 상태: 잠김
            _lockImage.gameObject.SetActive(true);
            SetRewardButtonAlpha(0.3f);
            _rewardBtn.interactable = false;
            return;
        }
        
        bool isUnlocked = _achievementManager.IsAchievementUnlocked(data.steamAchievementId);
        bool isRewardClaimed = _achievementManager.IsRewardClaimed(data.steamAchievementId);
        
        if (isRewardClaimed)
        {
            // 보상 수령 완료: 자물쇠 없음, 보석 회색 톤
            _lockImage.gameObject.SetActive(false);
            SetRewardButtonAlpha(0.5f);
            SetRewardButtonColor(Color.gray);
            _rewardBtn.interactable = false;
        }
        else if (isUnlocked)
        {
            // 업적 달성, 보상 수령 가능: 자물쇠 없음, 보석 밝게
            _lockImage.gameObject.SetActive(false);
            SetRewardButtonAlpha(1f);
            SetRewardButtonColor(Color.white);
            _rewardBtn.interactable = true;
        }
        else
        {
            // 업적 미달성: 자물쇠 표시, 보석 어둡게
            _lockImage.gameObject.SetActive(true);
            SetRewardButtonAlpha(0.5f);
            SetRewardButtonColor(Color.white);
            _rewardBtn.interactable = false;
        }
    }

    /// <summary>
    /// 보상 버튼 이미지 투명도 설정
    /// </summary>
    void SetRewardButtonAlpha(float alpha)
    {
        if (_rewardBtn != null)
        {
            Image buttonImage = _rewardBtn.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color color = buttonImage.color;
                color.a = alpha;
                buttonImage.color = color;
            }
        }
    }

    /// <summary>
    /// 보상 버튼 이미지 색상 설정
    /// </summary>
    void SetRewardButtonColor(Color color)
    {
        if (_rewardBtn != null)
        {
            Image buttonImage = _rewardBtn.GetComponent<Image>();
            if (buttonImage != null)
            {
                Color currentColor = buttonImage.color;
                color.a = currentColor.a; // 투명도는 유지
                buttonImage.color = color;
            }
        }
    }



    void HighlightSelectedButton()
    {
        // 모든 버튼의 하이라이트 제거
        for (int i = 0; i < _generatedButtons.Count; i++)
        {
            var button = _generatedButtons[i];
            if (button != null)
            {
                // 선택된 버튼에 하이라이트 추가 (예시: 스케일 변경)
                if (i == _currentIndex)
                {
                    button.transform.localScale = Vector3.one * 1.1f;
                }
                else
                {
                    button.transform.localScale = Vector3.one;
                }
            }
        }
    }

    string GetCategoryDisplayName(Category category)
    {
        switch (category)
        {
            case Category.Stage: return stageCategoryTitle;
            case Category.Character: return characterCategoryTitle;
            case Category.Item: return itemCategoryTitle;
            default: return "업적";
        }
    }

    void OnRewardPressed()
    {
        var achievements = GetCurrentAchievements();
        if (_currentIndex >= achievements.Count) return;
        
        var data = achievements[_currentIndex];
        
        // 업적 달성 여부와 보상 수령 여부 확인
        bool isUnlocked = _achievementManager.IsAchievementUnlocked(data.steamAchievementId);
        bool isRewardClaimed = _achievementManager.IsRewardClaimed(data.steamAchievementId);
        
        if (isUnlocked && !isRewardClaimed)
        {
            // 보상 지급
            GiveReward(data.reward);
            
            // 상태 업데이트 (서버에 저장)
            _achievementManager.ClaimReward(data.steamAchievementId);
            
            // UI 갱신 (버튼 색상과 상세 정보 모두 갱신)
            RefreshAllUI();
            
            Debug.Log($"보상 획득: {data.reward}");
        }
        else
        {
            Debug.LogWarning("보상을 받을 수 없습니다. 업적 미달성 또는 이미 수령 완료");
        }
    }

    void RefreshAllUI()
    {
        // 버튼들의 색상 업데이트
        var achievements = GetCurrentAchievements();
        for (int i = 0; i < _generatedButtons.Count && i < achievements.Count; i++)
        {
            UpdateButtonAppearance(_generatedButtons[i], achievements[i]);
        }
        
        // 상세 정보 업데이트 (진행률 및 보상 상태 포함)
        RefreshDetailDisplay();
    }

    /// <summary>
    /// 보상 지급 메서드
    /// </summary>
    void GiveReward(string reward)
    {
        if (string.IsNullOrEmpty(reward)) return;
        
        // "50 Gems", "100 Gold" 등의 형식 파싱
        if (reward.Contains("Gems"))
        {
            int gemAmount = ExtractNumber(reward);
            if (gemAmount > 0)
            {
                _currencyManager.AddGems(gemAmount);
                Debug.Log($"젬 {gemAmount}개 획득!");
            }
        }
        else if (reward.Contains("Gold"))
        {
            int goldAmount = ExtractNumber(reward);
            if (goldAmount > 0)
            {
                _currencyManager.AddGold(goldAmount);
                Debug.Log($"골드 {goldAmount}개 획득!");
            }
        }
        else
        {
            // 기타 보상 (특별 아이템, 캐릭터 등)
            Debug.Log($"특별 보상 획득: {reward}");
        }
    }

    /// <summary>
    /// 문자열에서 숫자 추출
    /// </summary>
    int ExtractNumber(string text)
    {
        string numberString = "";
        foreach (char c in text)
        {
            if (char.IsDigit(c))
            {
                numberString += c;
            }
        }
        
        if (int.TryParse(numberString, out int result))
        {
            return result;
        }
        
        return 0;
    }

    protected override void OnDestroy()
    {
        // 생성된 버튼들 정리
        ClearGeneratedButtons();
    }

    #region Context Menu 테스트 메서드

    /// <summary>
    /// 에디터에서 업적 테스트 - 첫 번째 스테이지 클리어
    /// </summary>
    [ContextMenu("Test Achievement - First Stage Clear")]
    public void TestFirstStageClear()
    {
        // AchievementIntegration을 통해 스테이지 클리어 이벤트 발생
        AchievementIntegration.OnStageCleared(1, 25f, 2, 3, 3);
        Debug.Log("[UI_Achievement] 테스트: 첫 번째 스테이지 클리어 시뮬레이션");
    }

    /// <summary>
    /// 에디터에서 업적 테스트 - 아이템 구매
    /// </summary>
    [ContextMenu("Test Achievement - Item Purchase")]
    public void TestItemPurchase()
    {
        // AchievementIntegration을 통해 아이템 구매 이벤트 발생
        AchievementIntegration.OnItemPurchased("TestItem");
        Debug.Log("[UI_Achievement] 테스트: 아이템 구매 시뮬레이션");
    }

    /// <summary>
    /// 에디터에서 업적 테스트 - 완벽 클리어
    /// </summary>
    [ContextMenu("Test Achievement - Perfect Clear")]
    public void TestPerfectClear()
    {
        // 무사망, 모든 젬 수집으로 완벽 클리어 시뮬레이션
        AchievementIntegration.OnStageCleared(5, 120f, 0, 3, 3);
        Debug.Log("[UI_Achievement] 테스트: 완벽 클리어 시뮬레이션");
    }

    /// <summary>
    /// 에디터에서 업적 테스트 - 스피드 클리어
    /// </summary>
    [ContextMenu("Test Achievement - Speed Clear")]
    public void TestSpeedClear()
    {
        // 30초 이내 클리어 시뮬레이션
        AchievementIntegration.OnStageCleared(3, 25f, 1, 2, 3);
        Debug.Log("[UI_Achievement] 테스트: 스피드 클리어 시뮬레이션");
    }

    /// <summary>
    /// Steam 설정 다시 적용
    /// </summary>
    [ContextMenu("Reapply Steam Settings")]
    public void ReapplySteamSettings()
    {
        ApplySteamSettings();
    }

    /// <summary>
    /// 현재 업적 진행률 출력
    /// </summary>
    [ContextMenu("Print Achievement Progress")]
    public void PrintAchievementProgress()
    {
        AchievementIntegration.PrintCurrentProgress();
    }

    #endregion
}

[Serializable]
public class AchievementData
{
    public string title;
    [TextArea] public string description;
    public string reward; // 보상 텍스트
    public string steamAchievementId; // Steam 업적 ID
    // 보상 수령 여부는 AchievementManager에서 관리
}
