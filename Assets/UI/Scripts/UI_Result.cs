using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using JustClimb.Manager;  

public class UI_Result : UI_Popup
{
    // 자동 바인딩용 enum
    enum Images { Gem1, Gem2, Gem3 }
    enum Texts { TimeText, DeathText }
    enum Sliders { TimerSlider }
    enum Buttons { MainMenu, NextStage, LobbyMenu }

    Image[] _gems = new Image[3];
    TMP_Text _timeText;
    TMP_Text _deathText;
    Slider _timerSlider;
    Button _btnMainMenu;
    Button _btnNextStage;
    Button _btnLobbyMenu;

    void Awake()
    {
        Init();
    }

    public override void Init()
    {
        base.Init();

        // 1) 바인딩
        Bind<Image>(typeof(Images));
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Slider>(typeof(Sliders));
        Bind<Button>(typeof(Buttons));

        // 2) 레퍼런스 가져오기
        for (int i = 0; i < 3; i++)
            _gems[i] = GetImage((int)Images.Gem1 + i);

        _timeText = GetText((int)Texts.TimeText);
        _deathText = GetText((int)Texts.DeathText);
        _timerSlider = Get<Slider>((int)Sliders.TimerSlider);

        _btnMainMenu = GetButton((int)Buttons.MainMenu);
        _btnNextStage = GetButton((int)Buttons.NextStage);
        _btnLobbyMenu = GetButton((int)Buttons.LobbyMenu);

        // 3) 버튼 이벤트
        _btnMainMenu.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("nextScene", Managers.Scene.GetSceneName(Define.Scene.Main));
            Managers.Scene.LoadScene(Define.Scene.Loading);
        });
        _btnNextStage.onClick.AddListener(GoToNextStage);
        _btnLobbyMenu.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("nextScene", Managers.Scene.GetSceneName(Define.Scene.Lobby));
            Managers.Scene.LoadScene(Define.Scene.Loading);
        });
    }

    // UI 띄우고, 보상·저장·UI 표시를 모두 처리.
    public void ShowResult(TimeSpan elapsed)
    {
        // 시간·죽음수 표시
        _timeText.text = $"Time: {elapsed.Minutes:00}:{elapsed.Seconds:00}";
        _deathText.text = $"Deaths: {Managers.Game.PlayerDeathCount}";

        // 슬라이더: 0 ~ 1200초(20분)
        // 초기엔 maxValue가 1200이고 value도 1200
        _timerSlider.maxValue = 600f;
        _timerSlider.value = Mathf.Clamp(600f - (float)elapsed.TotalSeconds, 0f, 600f);

        // 보석 개수 계산 & 지급
        // 경과 시간(초)을 변수에 담고
        double totalSec = elapsed.TotalSeconds;

        // 보석 개수 계산
        int gemCount;
        if (totalSec < 300) gemCount = 3;  // 5분 미만
        else if (totalSec < 600) gemCount = 2;  // 10분 미만
        else gemCount = 1;  // 그 이후

        ItemManager.Instance.AddGems(gemCount);

        // 보석 UI & 애니메이션
        for (int i = 0; i < 3; i++)
        {
            bool give = i < gemCount;
            _gems[i].canvasRenderer.SetAlpha(give ? 1f : 0f);
            if (give) StartCoroutine(AnimateGem(_gems[i]));
        }

        // 5) 스테이지 클리어 저장
        SaveStageClear(elapsed, gemCount);
    }

    private void SaveStageClear(TimeSpan duration, int gemCount)
    {
        string name = SceneManager.GetActiveScene().name;
        if (!name.StartsWith("Stage") ||
            !int.TryParse(name.Substring(5), out int stageNum))
            return;

        // 이전 최고 보상 개수 불러오기
        int prevBest = PlayerPrefs.GetInt($"BestGemCount{stageNum}", 0);

        // 차액 계산 & 지급
        int delta = Mathf.Max(0, gemCount - prevBest);
        if (delta > 0)
            ItemManager.Instance.AddGems(delta);

        // 최고 보상 개수 갱신
        if (gemCount > prevBest)
            PlayerPrefs.SetInt($"BestGemCount{stageNum}", gemCount);

        // 기존 클리어 타임 저장
        int newTime = (int)duration.TotalSeconds;
        int oldTime = PlayerPrefs.GetInt($"ClearTime{stageNum}", int.MaxValue);
        if (newTime < oldTime)
            PlayerPrefs.SetInt($"ClearTime{stageNum}", newTime);

        // 스테이지 클리어 플래그 (잠금 해제)
        PlayerPrefs.SetInt($"Stage{stageNum}", 1);

        PlayerPrefs.Save();
    }

    private IEnumerator AnimateGem(Image gem)
    {
        float dur = 0.25f, max = 1.5f, min = 1f;
        while (true)
        {
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                gem.transform.localScale = Vector3.one * Mathf.Lerp(min, max, t / dur);
                yield return null;
            }
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                gem.transform.localScale = Vector3.one * Mathf.Lerp(max, min, t / dur);
                yield return null;
            }
        }
    }

    private void GoToNextStage()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (!scene.StartsWith("Stage")) return;
        int num = int.Parse(scene.Substring(5));
        Define.Scene next = (Define.Scene)Enum.Parse(typeof(Define.Scene), $"Stage{num + 1}");

        PlayerPrefs.SetString("nextScene", Managers.Scene.GetSceneName(next));
        Managers.Scene.LoadScene(Define.Scene.Loading);
    }
}
