using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[Serializable]
public class StageGemGroup
{
    // 각 스테이지의 보석 이미지를 Inspector에서 설정
    public List<Image> gems;
}

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

    public override void Init()
    {
        base.Init(); // Canvas 세팅, ESC 처리 자동 적용

        // Return 버튼 연결
        if (returnButton != null)
            returnButton.onClick.AddListener(ClosePopupUI);
    }

    private void Start()
    {
        Init();
        SetupStages();
    }

    private void SetupStages()
    {
        int count = stageButtons.Count;

        for (int i = 0; i < count; i++)
        {
            int stageIndex = i;
            int stageNumber = i + 1;

            // 1번 스테이지는 항상 오픈, 그 외는 이전 스테이지 클리어 여부로 판단
            bool unlocked = stageNumber == 1
                         || PlayerPrefs.GetInt("Stage" + (stageNumber - 1), 0) == 1;

            // 잠금 아이콘만 표시
            if (lockImages != null && stageIndex < lockImages.Count)
                lockImages[stageIndex].SetActive(!unlocked);

            // 기존 리스너 제거
            stageButtons[stageIndex].onClick.RemoveAllListeners();

            // 클릭 이벤트: 항상 허용, unlocked 여부로 분기
            stageButtons[stageIndex].onClick.AddListener(() =>
            {
                if (unlocked)
                {
                    GoToStageScene(stageNumber);
                }
                else
                {
                    Managers.UI
                        .ShowPopupUI<GenericInfoPopup>("Warning Panel")
                        .Setup(
                            "Warning!",
                            $"Clear Stage 1-{stageNumber - 1} first."
                        );
                }
            });
        }

        // 보상 보석 투명도 세팅 (기존 로직)
        for (int i = 0; i < stageGemGroups.Count; i++)
            SetGemAlphas(stageGemGroups[i].gems, i + 1);

        // 커서 보이기
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // 보석 이미지 투명도 세팅
    private void SetGemAlphas(List<Image> gems, int stageNumber)
    {
        const float defaultRawAlpha = 150f;  // 기본 raw 알파(0~255)
        for (int j = 0; j < gems.Count; j++)
        {
            // PlayerPrefs에 저장된 raw 알파(0~255) 값을 불러오거나 기본값 사용
            string key = $"Gem{stageNumber}_{j}";
            float rawAlpha = PlayerPrefs.GetFloat(key, defaultRawAlpha);

            // 0~1 범위로 정규화
            float a = Mathf.Clamp01(rawAlpha / 255f);

            // 이미지에 적용
            var col = gems[j].color;
            col.a = a;
            gems[j].color = col;
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
        PlayerPrefs.SetString("nextScene", Managers.Scene.GetSceneName(targetScene));

        //Managers.Sound.PlaySFX(0);
        Managers.Scene.LoadScene(Define.Scene.Loading);
        //Managers.Sound.PlayBGM(2);
    }
}
