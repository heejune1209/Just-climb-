using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class UI_Stage : UI_Scene
{
    enum Gameobjects
    {
        // 인벤토리 UI
        Inventory,
    }

    // 화면에 띄울 텍스트 필드들
    enum Texts
    {
        DeathCountText,  // 플레이어 데스 카운트
        TimerText,       // 경과 시간
    }

    private GameObject _inventory;
    private TMP_Text _deathCountText;
    private TMP_Text _timerText;
    


    // **튜토리얼이 이미 표시됐는지** PlayerPrefs 키
    private const string TUTORIAL_KEY = "TutorialDisplayed";

    void Awake()
    {
        Init();
        
        // 혹시몰라서 초기화 
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public override void Init()
    {
        base.Init();

        // 1) 바인딩
        Bind<GameObject>(typeof(Gameobjects));
        Bind<TextMeshProUGUI>(typeof(Texts));

        _inventory = GetGameObject((int)Gameobjects.Inventory);
        _deathCountText = GetText((int)Texts.DeathCountText);
        _timerText = GetText((int)Texts.TimerText);

        // 2) 고정 UI (인벤토리)
        Managers.UI.ShowSceneUI<UI_Inventory>("UI_Inventory");

        // 3) GameManager 이벤트 구독
        Managers.Game.OnDeathCountChanged += UpdateDeathCount;
        Managers.Game.OnTimerUpdated += UpdateTimerText;

        // **초기값 한 번 뿌려주기**
        UpdateDeathCount(Managers.Game.PlayerDeathCount);
        UpdateTimerText(TimeSpan.Zero);
    }

    void Update()
    {
        var tab = Keyboard.current.tabKey;

        // Tab 누를 때만 정보창 띄우기 (Warning 창이 떠 있으면 무시)
        if (tab.wasPressedThisFrame
            && !Managers.UI.IsPopupOpen<UI_Information>()
            && !Managers.UI.IsPopupOpen<UI_Warning>())
        {
            Managers.UI.ShowPopupUI<UI_Information>("UI_Information"); 
        }

        // Tab 뗄 때, 맨 위 팝업이 정보창이면 닫아주기
        if (tab.wasReleasedThisFrame)
        {
            // GetTopPopup()이 UI_Information인 경우에만
            if (Managers.UI.GetTopPopup() is UI_Information)
            {
                Managers.UI.ClosePopupUI();               
            }
                
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame &&
            Managers.UI.GetTopPopup() == null)
        {            
            Managers.UI.ShowPopupUI<UI_Warning>("UI_Warning_Stage");
        }
    }

    // 데스 카운트 업데이트 핸들러
    private void UpdateDeathCount(int count)
    {
        if (_deathCountText != null)
            _deathCountText.text = $"Death : {count}";
    }

    // 타이머 텍스트 업데이트 핸들러
    private void UpdateTimerText(TimeSpan elapsed)
    {
        if (_timerText != null)
            _timerText.text = $"Time : {elapsed.Minutes:00} : {elapsed.Seconds:00}";
    }

    void OnDestroy()
    {
        // 이벤트 해제
        Managers.Game.OnDeathCountChanged -= UpdateDeathCount;
        Managers.Game.OnTimerUpdated -= UpdateTimerText;
    }

    // 트리거에서 호출: Stage1 특정 Tutorial 팝업 (한 번만)
    public void ShowTutorial()
    {
        // 이미 팝업을 띄운 적이 있으면 종료
        if (PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1)
            return;
               
        // 처음 띄우는 순간
        Managers.UI.ShowPopupUI<GenericInfoPopup>("UI_Tutorial");

        // 다시는 띄우지 않도록 저장
        PlayerPrefs.SetInt(TUTORIAL_KEY, 1);
        PlayerPrefs.Save();
    }

    // GoalTrigger에서 호출: 결과 팝업
    public void ShowResult()
    {
        var popup = Managers.UI.ShowPopupUI<UI_Result>("UI_Result");
        popup.ShowResult(Managers.Game.ElapsedTime());
    }
}
