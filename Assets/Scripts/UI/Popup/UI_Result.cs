using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using JustClimb.Manager;
using Zenject;
using System.Collections.Generic; 

// 책임: 
//  결과 화면 그리기(직전/최고 기록), 보석 애니메이션,
//  버튼 클릭으로 씬 전환, 보상 지급, 스테이지 완료 처리
public class UI_Result : UI_Popup
{
    // DI 주입받을 매니저들
    [Inject] private ISceneManagerEx _sceneManager;
    [Inject] private IGameManager _gameManager;
    [Inject] private IStageManager _stageManager;

    // 자동 바인딩용 enum
    enum Images { Gem1, Gem2, Gem3 }
    enum Texts { TimeText, DeathText, BestTimeText, BestDeathText }
    enum Sliders { TimerSlider }
    enum Buttons { MainMenu, NextStage, LobbyMenu }

    Image[] _gems = new Image[3];
    TMP_Text _timeText;
    TMP_Text _deathText;
    TMP_Text _bestTimeText;
    TMP_Text _bestDeathText;
    Slider _timerSlider;
    Button _btnMainMenu;
    Button _btnNextStage;
    Button _btnLobbyMenu;

    bool _initialized = false;

    // 애니메이션 코루틴 추적용
    private List<Coroutine> _activeCoroutines = new List<Coroutine>();

    private void Start()
    {
        Init();
    }

    public override void Init()
    {
        // Init 이 아직 안 됐으면 한 번만 보장 호출
        if (_initialized) return;
        _initialized = true;

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
        _bestTimeText = GetText((int)Texts.BestTimeText);
        _bestDeathText = GetText((int)Texts.BestDeathText);
        _timerSlider = Get<Slider>((int)Sliders.TimerSlider);

        _btnMainMenu = GetButton((int)Buttons.MainMenu);
        _btnNextStage = GetButton((int)Buttons.NextStage);
        _btnLobbyMenu = GetButton((int)Buttons.LobbyMenu);

        // 3) 버튼 이벤트
        _btnMainMenu.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("nextScene", _sceneManager.GetSceneName(Define.Scene.Main));
            _sceneManager.LoadScene(Define.Scene.Loading);
        });
        _btnNextStage.onClick.AddListener(GoToNextStage);
        _btnLobbyMenu.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("nextScene", _sceneManager.GetSceneName(Define.Scene.Lobby));
            _sceneManager.LoadScene(Define.Scene.Loading);
        });
    }

    /// <summary>
    /// 결과 UI 표시 + 보상 지급 + 저장까지 처리합니다.
    /// </summary>
    public void ShowResult(TimeSpan elapsed)
    {
        try
        {
            Debug.Log($"[UI_Result] ShowResult 시작 - 경과시간: {elapsed}");
            
            // Init 이 아직 안 됐으면 한 번만 보장 호출
            if (!_initialized) Init();

            // 기존 애니메이션 정리
            StopAllAnimations();

            // 1) 시간·사망수 표시
            _timeText.text = $"Time: {elapsed.Minutes:00}:{elapsed.Seconds:00}";
            _deathText.text = $"Deaths: {_gameManager.PlayerDeathCount}";

            // 씬 이름에서 스테이지 번호 파싱
            string scene = SceneManager.GetActiveScene().name;
            int stageNum = 0;
            if (scene.StartsWith("Stage") && int.TryParse(scene.Substring(5), out var n))
                stageNum = n;

            if (stageNum <= 0)
            {
                Debug.LogError("[UI_Result] 유효하지 않은 스테이지 번호");
                return;
            }

            Debug.Log($"[UI_Result] 스테이지 {stageNum} 결과 처리 시작");

            // 2) 타이머 슬라이더 세팅 (이건 Best와 보상 전에도 보여줘도 무방)
            _timerSlider.maxValue = 600f;
            _timerSlider.value = Mathf.Clamp(600f - (float)elapsed.TotalSeconds, 0f, 600f);

            // 3) 보석 개수 계산 및 애니메이션
            int gemCount = elapsed.TotalSeconds < 300 ? 3
                         : elapsed.TotalSeconds < 600 ? 2
                         : 1;
                         
            Debug.Log($"[UI_Result] 젬 개수: {gemCount}");
            
            for (int i = 0; i < 3; i++)
            {
                bool visible = i < gemCount;
                _gems[i].canvasRenderer.SetAlpha(visible ? 1f : 0f);
                if (visible)
                {
                    var coroutine = StartCoroutine(AnimateGem(_gems[i], 5f)); // 5초 동안만 애니메이션
                    _activeCoroutines.Add(coroutine);
                }
            }

            // 4) 보상 지급 (여기서 SetCleared 호출)
            Debug.Log($"[UI_Result] SetCleared 호출 시작");
            _stageManager.SetCleared(stageNum, gemCount, (float)elapsed.TotalSeconds, _gameManager.PlayerDeathCount);
            _gameManager.OnStageCleared();
            
            Debug.Log($"[UI_Result] SetCleared 호출 완료");

            // 5) 보상 직후, 업데이트된 Best 기록을 다시 읽어와 UI에 반영
            float bestSec = _stageManager.GetBestTime(stageNum);
            int bestDeaths = _stageManager.GetBestDeath(stageNum);

            if (bestSec < float.MaxValue)
            {
                var bt = TimeSpan.FromSeconds(bestSec);
                _bestTimeText.text = $"Best Time: {bt.Minutes:00}:{bt.Seconds:00}";
            }
            else
            {
                _bestTimeText.text = "Best Time: -- : --";
            }

            _bestDeathText.text = bestDeaths < int.MaxValue
                ? $"Best Deaths: {bestDeaths}"
                : "Best Deaths: --";
                
            Debug.Log($"[UI_Result] ShowResult 완료");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UI_Result] ShowResult에서 예외 발생: {e.Message}\n{e.StackTrace}");
        }
    }

    private IEnumerator AnimateGem(Image gem, float duration)
    {
        float dur = 0.25f, max = 1.5f, min = 1f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            for (float t = 0; t < dur && elapsed < duration; t += Time.unscaledDeltaTime)
            {
                gem.transform.localScale = Vector3.one * Mathf.Lerp(min, max, t / dur);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            for (float t = 0; t < dur && elapsed < duration; t += Time.unscaledDeltaTime)
            {
                gem.transform.localScale = Vector3.one * Mathf.Lerp(max, min, t / dur);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        
        // 애니메이션 종료 시 원래 크기로 복원
        gem.transform.localScale = Vector3.one;
    }

    private void StopAllAnimations()
    {
        foreach (var coroutine in _activeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        _activeCoroutines.Clear();
        
        // 모든 젬 크기 원래대로 복원
        for (int i = 0; i < 3; i++)
        {
            if (_gems[i] != null)
            {
                _gems[i].transform.localScale = Vector3.one;
            }
        }
    }

    protected override void OnDestroy()
    {
        // 애니메이션 정리
        StopAllAnimations();
        base.OnDestroy();
    }

    private void GoToNextStage()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (!scene.StartsWith("Stage")) return;
        int num = int.Parse(scene.Substring(5));
        Define.Scene next = (Define.Scene)Enum.Parse(typeof(Define.Scene), $"Stage{num + 1}");

        PlayerPrefs.SetString("nextScene", _sceneManager.GetSceneName(next));
        _sceneManager.LoadScene(Define.Scene.Loading);
    }
}
