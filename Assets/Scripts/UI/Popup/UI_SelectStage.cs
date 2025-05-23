using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using JustClimb.Manager;

[Serializable]
public class StageGemGroup
{
    // 각 스테이지의 보석 이미지를 Inspector에서 설정
    public List<Image> gems;
}

// 책임:
// 버튼 잠금·언락 표시(IsUnlocked)
// 과거 보석 보상 표시(GetBestReward)
// 스테이지 선택 시 씬 전환(SceneManagerEx.LoadScene)
public class UI_SelectStage : UI_Popup
{
    [Header("Stage Buttons")]
    [SerializeField] private List<Button> stageButtons;   // 스테이지 1~10 버튼

    [Header("Lock Images")]
    [SerializeField] private List<GameObject> lockImages; // 스테이지 잠금 아이콘

    [Header("Gem Rewards")]
    [SerializeField] private List<StageGemGroup> stageGemGroups; // 스테이지별 보석 이미지 그룹

    [Header("Return Button")]
    [SerializeField] private Button returnButton;       // 팝업 닫기 버튼

    private StageManager _stageMgr;

    public override void Init()
    {
        base.Init(); // Canvas 세팅, ESC 처리 등

        _stageMgr = Managers.Instance.Stage;

        // 1) 팝업 닫기 버튼
        if (returnButton != null)
            returnButton.onClick.AddListener(ClosePopupUI);

        // 2) 스테이지 언락/보상 변경 이벤트 구독
        _stageMgr.OnStageUnlocked += OnStageUnlocked;
        _stageMgr.OnBestRewardUpdated += OnBestRewardUpdated;

        // 3) 최초 UI 셋업
        SetupStages();
    }

    void Awake()
    {
        Init();
    }


    private void OnDestroy()
    {
        // 언구독(clean up)
        if (_stageMgr != null)
        {
            _stageMgr.OnStageUnlocked -= OnStageUnlocked;
            _stageMgr.OnBestRewardUpdated -= OnBestRewardUpdated;
        }
    }

    private void Start()
    {
        // SetupStages는 Init() 안에서도 호출해 줘도 좋습니다.
        // SetupStages();
    }


    /// <summary>
    /// 팝업 오픈 시 전체 스테이지 버튼/잠금/보석 상태를 한 번에 초기화
    /// </summary>
    private void SetupStages()
    {
        int count = stageButtons.Count;

        for (int i = 0; i < count; i++)
        {
            int stageNum = i + 1;
            bool unlocked = _stageMgr.IsUnlocked(stageNum);
            int bestReward = _stageMgr.GetBestReward(stageNum);

            // 버튼 활성화/잠금 아이콘
            stageButtons[i].interactable = unlocked;
            if (i < lockImages.Count)
                lockImages[i].SetActive(!unlocked);

            // 클릭 핸들러
            int idx = i;
            stageButtons[i].onClick.RemoveAllListeners();
            stageButtons[i].onClick.AddListener(() =>
            {
                if (_stageMgr.IsUnlocked(idx + 1))
                    GoToStageScene(idx + 1);
                else
                    ShowLockedWarning(idx + 1);
            });

            // 보석 투명도 초기화
            if (i < stageGemGroups.Count)
                SetGemAlphas(i, bestReward);
        }

        // 커서
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

        stageButtons[idx].interactable = true;
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

        // 해당 인덱스의 Group 이 유효한지 체크
        if (stageIndex < 0 || stageIndex >= stageGemGroups.Count) return;

        var gems = stageGemGroups[stageIndex].gems;

        for (int j = 0; j < gems.Count; j++)
        {
            float alpha;
            if (!unlocked)
                alpha = 0.3f;
            else
                alpha = (j < bestReward) ? 1f : 0.3f;

            gems[j].canvasRenderer.SetAlpha(alpha);
        }
    }

    // 스테이지 이동 로직
    private void GoToStageScene(int stageNumber)
    {
        // Define.Scene에 추가된 Stage1~Stage10 enum 값 사용
        string enumName = $"Stage{stageNumber}";
        if (!Enum.TryParse(typeof(Define.Scene), enumName, out var sceneEnum))
        {
            Debug.LogError($"Invalid scene enum: {enumName}");
            return;
        }
        var targetScene = (Define.Scene)sceneEnum;

        // 다음에 로드할 씬 이름 기록 (로딩 씬을 거치는 경우)
        PlayerPrefs.SetString("nextScene", Managers.Instance.Scene.GetSceneName(targetScene));

        //Managers.Sound.PlaySFX(0);
        Managers.Instance.Scene.LoadScene(Define.Scene.Loading);
        //Managers.Sound.PlayBGM(2);
    }

    private void ShowLockedWarning(int stageNumber)
    {
        Managers.Instance.UI
            .ShowPopupUI<GenericInfoPopup>("Warning Panel")
            .Setup("Warning!",
                   $"Clear Stage 1–{stageNumber - 1} first.");
    }
}
