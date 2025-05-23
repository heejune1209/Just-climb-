using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// 팝업 형태로 업적 UI를 관리하는 스크립트
public class UI_Achievement : UI_Popup
{
    // 바인딩할 버튼
    enum Buttons
    {
        CloseButton,        // 팝업 닫기
        RewardButton,       // 보상 받기
        StageTabButton,     // 스테이지 카테고리
        CharacterTabButton, // 캐릭터 카테고리
        ItemTabButton       // 아이템 카테고리
    }

    // 바인딩할 텍스트
    enum Texts
    {
        CategoryText,   // 현재 카테고리 제목
        TitleText,      // 업적 타이틀
        DescText,       // 업적 설명
        //PageText,       // 현재 페이지 표시
        RewardText      // 보상 텍스트
    }

    // 바인딩할 패널
    enum GameObjects
    {
        ContentRoot     // 업적 정보 표시할 Root (버튼 리스트의 부모)
    }

    [Header("Entry Buttons (Inspector)")]
    [Tooltip("각 카테고리당 최대 3개의 업적 버튼을 드래그하세요.")]
    [SerializeField] List<Button> entryButtons;

    [Header("업적 데이터")]
    public List<AchievementData> stageAchievements = new List<AchievementData>();
    public List<AchievementData> characterAchievements = new List<AchievementData>();
    public List<AchievementData> itemAchievements = new List<AchievementData>();

    // 카테고리 정의
    enum Category { Stage, Character, Item }
    Category _currentCategory = Category.Stage;
    int _currentIndex = 0;

    // 바인딩 참조
    TMP_Text _categoryTitle;
    TMP_Text _title;
    TMP_Text _desc;
    TMP_Text _page;
    TMP_Text _reward;
    Button _rewardBtn;
    Transform _contentRoot;

    private void Start()
    {
        Init();
        // 바인딩된 컴포넌트 참조
        _categoryTitle = GetText((int)Texts.CategoryText);
        _title = GetText((int)Texts.TitleText);
        _desc = GetText((int)Texts.DescText);
        //_page = GetText((int)Texts.PageText);
        _reward = GetText((int)Texts.RewardText);
        _contentRoot = GetGameObject((int)GameObjects.ContentRoot).transform;

        _rewardBtn = GetButton((int)Buttons.RewardButton);
        _rewardBtn.onClick.AddListener(() =>
        {
            Managers.Instance.Sound.PlaySFX(0);    // 클릭 사운드
            OnRewardPressed();
        });

        // 카테고리 탭 버튼
        GetButton((int)Buttons.StageTabButton).onClick.AddListener(() =>
        {
            Managers.Instance.Sound.PlaySFX(0);
            SwitchCategory(Category.Stage);
        });
        GetButton((int)Buttons.CharacterTabButton).onClick.AddListener(() =>
        {
            Managers.Instance.Sound.PlaySFX(0);
            SwitchCategory(Category.Character);
        });
        GetButton((int)Buttons.ItemTabButton).onClick.AddListener(() =>
        {
            Managers.Instance.Sound.PlaySFX(0);
            SwitchCategory(Category.Item);
        });

        PopulateEntryButtons();
        ShowDetail(0);
    }

    public override void Init()
    {
        base.Init();
        // 자동 바인딩
        Bind<Button>(typeof(Buttons));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<GameObject>(typeof(GameObjects));
        // 닫기 버튼
        GetButton((int)Buttons.CloseButton).gameObject.BindEvent(_ =>
        {
            Managers.Instance.Sound.PlaySFX(0);
            ClosePopupUI();
        });
    }

    // 엔트리 버튼 리스트 생성 및 바인딩
    void PopulateEntryButtons()
    {
        var list = GetCurrentList();
        for (int i = 0; i < entryButtons.Count; i++)
        {
            var btn = entryButtons[i];
            if (i < list.Count)
            {
                btn.gameObject.SetActive(true);
                var label = btn.GetComponentInChildren<TextMeshProUGUI>();
                label.text = list[i].title;
                btn.onClick.RemoveAllListeners();
                int idx = i;
                btn.onClick.AddListener(() =>
                {
                    Managers.Instance.Sound.PlaySFX(0);
                    ShowDetail(idx);
                });
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }
    }

    void ShowDetail(int index)
    {
        _currentIndex = index;
        RefreshUI();
    }

    void RefreshUI()
    {
        var list = GetCurrentList();
        if (list.Count == 0)
            return;
        _currentIndex = Mathf.Clamp(_currentIndex, 0, list.Count - 1);
        // 카테고리 제목
        _categoryTitle.text = _currentCategory.ToString();
        var data = list[_currentIndex];
        _title.text = data.title;
        _desc.text = data.description;
        //_page.text = string.Format("{0}/{1}", _currentIndex + 1, list.Count);
        _reward.text = data.reward;
    }

    void OnRewardPressed()
    {
        var data = GetCurrentList()[_currentIndex];
        Debug.Log($"Reward collected: {data.reward}");
    }

    void SwitchCategory(Category cat)
    {
        _currentCategory = cat;
        _currentIndex = 0;
        PopulateEntryButtons();
        RefreshUI();
    }

    List<AchievementData> GetCurrentList()
    {
        switch (_currentCategory)
        {
            case Category.Stage: return stageAchievements;
            case Category.Character: return characterAchievements;
            case Category.Item: return itemAchievements;
            default: return stageAchievements;
        }
    }
}

[System.Serializable]
public class AchievementData
{
    public string title;
    [TextArea] public string description;
    public string reward; // 보상 텍스트
}
