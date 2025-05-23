using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using JustClimb.Manager;

// 책임: 
// 결과 화면 그리기(시간, 사망 수, 보석 애니메이션)
// 버튼 클릭으로 씬 전환
// 보상 지급 로직(CurrencyManager.AddGems) 호출
// 스테이지 완료 처리(StageManager.SetCleared) 호출
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
            PlayerPrefs.SetString("nextScene", Managers.Instance.Scene.GetSceneName(Define.Scene.Main));
            Managers.Instance.Scene.LoadScene(Define.Scene.Loading);
        });
        _btnNextStage.onClick.AddListener(GoToNextStage);
        _btnLobbyMenu.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("nextScene", Managers.Instance.Scene.GetSceneName(Define.Scene.Lobby));
            Managers.Instance.Scene.LoadScene(Define.Scene.Loading);
        });
    }

    /// <summary>
    /// 결과 UI 표시 + 보상 지급 + 저장까지 처리합니다.
    /// </summary>
    public void ShowResult(TimeSpan elapsed)
    {
        // 1) 시간·사망수 표시
        _timeText.text = $"Time: {elapsed.Minutes:00}:{elapsed.Seconds:00}";
        _deathText.text = $"Deaths: {Managers.Instance.Game.PlayerDeathCount}";

        // 2) 타이머 슬라이더
        _timerSlider.maxValue = 600f;
        _timerSlider.value = Mathf.Clamp(
            600f - (float)elapsed.TotalSeconds, 0f, 600f
        );

        // 3) 보석 개수 계산
        int gemCount = (elapsed.TotalSeconds < 300) ? 3
                     : (elapsed.TotalSeconds < 600) ? 2
                     : 1;

        // 4) CurrencyManager 통해 보석 지급
        Managers.Instance.Currency.AddGems(gemCount);

        // 5) 보석 애니메이션
        for (int i = 0; i < 3; i++)
        {
            bool visible = i < gemCount;
            _gems[i].canvasRenderer.SetAlpha(visible ? 1f : 0f);
            if (visible)
                StartCoroutine(AnimateGem(_gems[i]));
        }

        // 6) StageManager에 클리어 저장 (플래그·보상·기록 갱신)
        string scene = SceneManager.GetActiveScene().name;
        if (scene.StartsWith("Stage") &&
            int.TryParse(scene.Substring(5), out int stageNum))
        {
            Managers.Instance.Stage
                .SetCleared(stageNum, gemCount, (int)elapsed.TotalSeconds);
        }
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

        PlayerPrefs.SetString("nextScene", Managers.Instance.Scene.GetSceneName(next));
        Managers.Instance.Scene.LoadScene(Define.Scene.Loading);
    }
}
