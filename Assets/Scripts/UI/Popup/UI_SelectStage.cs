using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;                           
using JustClimb.Manager;               

[Serializable]
public class StageGemGroup
{
    public List<Image> gems;
}

public class UI_SelectStage : UI_Popup
{
    [Header("Stage Buttons")]
    [SerializeField] private List<Button> stageButtons;

    [Header("Lock Images")]
    [SerializeField] private List<GameObject> lockImages;

    [Header("Gem Rewards")]
    [SerializeField] private List<StageGemGroup> stageGemGroups;

    [Header("Return Button")]
    [SerializeField] private Button returnButton;

    // --- DI 주입된 매니저들 -----------------------------------
    [Inject] private IStageManager _stageMgr;          // 스테이지 데이터 관리
    [Inject] private ISceneManagerEx _sceneManager;      // 씬 전환 관리
    // -------------------------------------------------------

    /// <summary>
    /// Zenject InjectGameObject 이후 호출되도록 Awake 대신 Start 사용
    /// </summary>
    void Start()
    {
        Init();    // UI_Popup.Init() → Canvas 셋업 후, 자식 Init 로직 실행
    }

    public override void Init()
    {
        base.Init();  // UI_Popup 쪽에서 Canvas 설정 :contentReference[oaicite:1]{index=1}

        // 1) 닫기 버튼 연결
        if (returnButton != null)
            returnButton.onClick.AddListener(ClosePopupUI);

        // 2) 이벤트 구독
        _stageMgr.OnStageUnlocked += OnStageUnlocked;       // DI로 받은 이벤트
        _stageMgr.OnBestRewardUpdated += OnBestRewardUpdated;

        // 3) 초기 UI 세팅
        SetupStages();
    }

    protected override void OnDestroy()
    {
        // 구독 해제
        if (_stageMgr != null)
        {
            _stageMgr.OnStageUnlocked -= OnStageUnlocked;
            _stageMgr.OnBestRewardUpdated -= OnBestRewardUpdated;
        }
    }

    /// <summary>
    /// 팝업 오픈 시 전체 스테이지 버튼/잠금/보석 상태를 한 번에 초기화
    /// </summary>
    private void SetupStages()
    {
        for (int i = 0; i < stageButtons.Count; i++)
        {
            int stageNum = i + 1;
            bool unlocked = _stageMgr.IsUnlocked(stageNum);           // 잠금 여부
            int bestReward = _stageMgr.GetBestReward(stageNum);

            // 버튼을 항상 interactable 하게 유지
            stageButtons[i].interactable = true;

            // lockImages 토글: 잠겨있으면 띄우고, 풀려있으면 숨김
            if (i < lockImages.Count)
                lockImages[i].SetActive(!unlocked);

            // 클릭 리스너 재등록: 잠금상태에 따라 분기
            stageButtons[i].onClick.RemoveAllListeners();
            int idx = i;
            stageButtons[i].onClick.AddListener(() =>
            {
                if (_stageMgr.IsUnlocked(idx + 1))
                {
                    GoToStageScene(idx + 1);
                }
                else
                {
                    ShowLockedWarning(idx + 1);
                }
            });

            // 보석 알파 세팅
            if (i < stageGemGroups.Count)
                SetGemAlphas(i, bestReward);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// 스테이지가 언락됐을 때 개별 버튼만 활성화
    /// </summary>
    private void OnStageUnlocked(int stageNum)
    {
        int idx = stageNum - 1;
        if (idx < 0 || idx >= stageButtons.Count) return;

        if (idx < lockImages.Count)
            lockImages[idx].SetActive(false);
    }

    /// <summary>
    /// 새로운 최고 보상이 갱신됐을 때 보석 투명도만 다시 그려줌
    /// </summary>
    private void OnBestRewardUpdated(int stageNum, int bestReward)
    {
        int idx = stageNum - 1;
        if (idx < 0 || idx >= stageGemGroups.Count) return;

        SetGemAlphas(idx, bestReward);
    }

    /// <summary>
    /// 보석 이미지의 알파(투명도) 세팅
    /// 잠금된 스테이지 보석은 모두 0.3f,
    /// 언락된 스테이지 보석은 획득 개수만큼 1f, 나머지는 0.3f
    /// </summary>
    private void SetGemAlphas(int stageIndex, int bestReward)
    {
        bool unlocked = _stageMgr.IsUnlocked(stageIndex + 1);
        var gems = stageGemGroups[stageIndex].gems;

        for (int j = 0; j < gems.Count; j++)
        {
            float alpha = !unlocked
                ? 0.3f
                : (j < bestReward ? 1f : 0.3f);
            gems[j].canvasRenderer.SetAlpha(alpha);
        }
    }

    // 스테이지 이동 로직
    private void GoToStageScene(int stageNumber)
    {
        // Define.Scene enum 변환
        string enumName = $"Stage{stageNumber}";
        if (!Enum.TryParse(typeof(Define.Scene), enumName, out var sceneEnum))
        {
            Debug.LogError($"Invalid scene enum: {enumName}");
            return;
        }
        var targetScene = (Define.Scene)sceneEnum;

        // 다음 씬 이름 저장
        PlayerPrefs.SetString("nextScene",
            _sceneManager.GetSceneName(targetScene));              // DI로 SceneName 조회

        _sceneManager.LoadScene(Define.Scene.Loading);            // DI로 씬 전환
    }

    private void ShowLockedWarning(int stageNumber)
    {
        _uiManager
            .ShowPopupUI<GenericInfoPopup>("Warning Panel")       // DI로 UI 매니저 사용
            .Setup("Warning!",
                   $"Clear Stage 1–{stageNumber - 1} first.");
    }
}
