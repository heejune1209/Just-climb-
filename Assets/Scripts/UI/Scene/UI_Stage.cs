using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Zenject;

// 인게임 HUD: 현재/최고/데스 카운트, 타이머 표시
public class UI_Stage : UI_Scene
{
    // DI 주입받을 매니저들
    [Inject] private IGameManager _gameManager;
    [Inject] private IStageManager _stageManager;  // ← 최고 기록 조회용
    [Inject] private IDataManager _dataManager;

    // 화면에 띄울 텍스트 필드들
    enum Texts
    {
        DeathCountText,  // 플레이어 데스 카운트
        TimerText,       // 경과 시간
        BestDeathText,   // ← 최고(최소) 데스 카운트
        BestTimeText     // ← 최고(최단) 타임
    }

    private TMP_Text _deathCountText;
    private TMP_Text _timerText;
    private TMP_Text _bestDeathText;
    private TMP_Text _bestTimeText;


    private void Start()
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
        Bind<TextMeshProUGUI>(typeof(Texts));

        _deathCountText = GetText((int)Texts.DeathCountText);
        _timerText = GetText((int)Texts.TimerText);
        _bestDeathText = GetText((int)Texts.BestDeathText);
        _bestTimeText = GetText((int)Texts.BestTimeText);

        // 2) 고정 UI (인벤토리)
        _uiManager.ShowSceneUI<UI_Inventory>("UI_Inventory");

        // 3) GameManager 이벤트 구독
        _gameManager.OnDeathCountChanged += UpdateDeathCount;
        _gameManager.OnTimerUpdated += UpdateTimerText;

        // 4) StageManager 이벤트 구독 (Best 기록)
        _stageManager.OnBestDeathUpdated += OnBestDeathUpdated;
        _stageManager.OnBestTimeUpdated += OnBestTimeUpdated;

        // **초기값 한 번 뿌려주기**
        UpdateDeathCount(_gameManager.PlayerDeathCount);
        UpdateTimerText(TimeSpan.Zero);

        // Best 기록은 이벤트에서 초기 DispatchAll 시 자동 업데이트됩니다.
        // 최초 한 번만, 로드된 저장 데이터로 Best 텍스트 갱신
        int stage = GetCurrentStageNum();
        // 데이터가 없으면 내부에서 -- 처리하니 안전
        OnBestDeathUpdated(stage, _stageManager.GetBestDeath(stage));
        OnBestTimeUpdated(stage, _stageManager.GetBestTime(stage));

    }

    void Update()
    {
        var tab = Keyboard.current.tabKey;

        // Tab 누를 때만 정보창 띄우기 (Warning 창이 떠 있으면 무시)
        if (tab.wasPressedThisFrame
            && !_uiManager.IsPopupOpen<UI_Information>()
            && !_uiManager.IsPopupOpen<UI_Warning>())
        {
            _uiManager.ShowPopupUI<UI_Information>("UI_Information");
        }

        // Tab 뗄 때, 맨 위 팝업이 정보창이면 닫아주기
        if (tab.wasReleasedThisFrame)
        {
            // GetTopPopup()이 UI_Information인 경우에만
            if (_uiManager.GetTopPopup() is UI_Information)
            {
                _uiManager.ClosePopupUI();
            }

        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame &&
            _uiManager.GetTopPopup() == null)
        {
            _uiManager.ShowPopupUI<UI_Warning>("UI_Warning_Stage");
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

    protected override void OnDestroy()
    {
        // 이벤트 해제
        _gameManager.OnDeathCountChanged -= UpdateDeathCount;
        _gameManager.OnTimerUpdated -= UpdateTimerText;
        _stageManager.OnBestDeathUpdated -= OnBestDeathUpdated;
        _stageManager.OnBestTimeUpdated -= OnBestTimeUpdated;
    }

    // 최소 데스
    private void OnBestDeathUpdated(int stage, int death)
    {
        if (GetCurrentStageNum() != stage) return;
        _bestDeathText.text = death < int.MaxValue
            ? $"Best Deaths : {death}"
            : "Best Deaths : --";
    }

    // 최단 타임
    private void OnBestTimeUpdated(int stage, float seconds)
    {
        if (GetCurrentStageNum() != stage) return;
        if (seconds < float.MaxValue)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            _bestTimeText.text = $"Best Time   : {ts.Minutes:00}:{ts.Seconds:00}";
        }
        else
        {
            _bestTimeText.text = "Best Time   : -- : --";
        }
    }
    private int GetCurrentStageNum()
    {
        string name = SceneManager.GetActiveScene().name;
        if (name.StartsWith("Stage")
            && int.TryParse(name.Substring(5), out int n))
            return n;
        return 0;
    }

    // 트리거에서 호출: Stage1 특정 Tutorial 팝업 (한 번만)
    public void ShowTutorial()
    {
        if (_dataManager.Current == null) return;

        // JSON 저장 데이터에서 플래그 확인
        if (_dataManager.Current.tutorialDisplayed)
            return;

        // 튜토리얼 팝업 띄우기
        _uiManager.ShowPopupUI<GenericInfoPopup>("UI_Tutorial");

        // 다시는 띄우지 않도록 JSON에 기록하고 저장
        _dataManager.Current.tutorialDisplayed = true;
        _dataManager.SaveLocal();

        // 튜토리얼 표시 여부 델타 생성
        _dataManager.GenerateDelta("tutorialDisplayed", true);
    }

    // GoalTrigger에서 호출: 결과 팝업
    public void ShowResult()
    {
        var popup = _uiManager.ShowPopupUI<UI_Result>("UI_Result");
        popup.ShowResult(_gameManager.ElapsedTime());
    }
}
