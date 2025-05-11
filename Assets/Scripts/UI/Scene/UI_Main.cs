using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine.SceneManagement;

// 메인 메뉴 UI 전체를 자동 바인딩하고 로직을 처리하는 스크립트
public class UI_Main : UI_Scene
{
    // 1) 바인딩할 버튼 종류
    enum Buttons
    {
        StartGame,        // 로비 씬으로 이동
        Settings,         // 설정 팝업 열기
        Achievements,     // 업적 씬으로 이동
        CharacterSelect,  // 캐릭터 선택 씬으로 이동
        ExitGame          // 게임 종료
    }

    // 2) 바인딩할 텍스트 종류
    enum Texts
    {
        TitleText         // 게임 타이틀 텍스트
    }

    // 3) 타이틀 폰트 펄스 효과 파라미터
    [Header("Title Pulse Settings")]
    [SerializeField] float titleMinSize = 10f;   // 최소 폰트 크기
    [SerializeField] float titleMaxSize = 20f;   // 최대 폰트 크기
    [SerializeField] float pulseSpeed = 1f;   // 펄스 속도

    // 내부 참조
    TMP_Text _titleComponent;
    Coroutine _pulseCoroutine;

    private void Start()
    {
        // 씬 UI 초기화
        Init();

        // 바인딩된 타이틀 텍스트 컴포넌트 가져오기
        _titleComponent = GetText((int)Texts.TitleText);

        // 폰트 펄스 이펙트 시작
        _pulseCoroutine = StartCoroutine(PulseTitle());
    }

    public override void Init()
    {
        // 기본 캔버스 세팅
        base.Init();

        // 텍스트, 버튼 자동 바인딩
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Button>(typeof(Buttons));

        // 타이틀 텍스트 설정
        GetText((int)Texts.TitleText).text = "Just Climb";

        // 버튼 클릭 이벤트 연결
        // 로비 씬으로 바로 가지 않고, 로딩 씬을 통해 넘어가도록 
        GetButton((int)Buttons.StartGame).gameObject.BindEvent(_ =>
        {
            Managers.Sound.PlaySFX(0);       // 클릭 효과음 (인덱스 0)
            PlayerPrefs.SetString("nextScene", Managers.Scene.GetSceneName(Define.Scene.Lobby));
            Managers.Scene.LoadScene(Define.Scene.Loading);
        });
        // 설정 팝업 열기
        GetButton((int)Buttons.Settings).gameObject.BindEvent(_ =>
        {
            Managers.Sound.PlaySFX(0);
            Managers.UI.ShowPopupUI<UI_Settings>("UI_Settings");
        });
        // 업적 씬으로 이동
        GetButton((int)Buttons.Achievements).gameObject.BindEvent(_ =>
        {
            Managers.Sound.PlaySFX(0);
            Managers.UI.ShowPopupUI<UI_Achievement>("UI_Achievement");
        });
        // 캐릭터 선택 씬으로 이동
        GetButton((int)Buttons.CharacterSelect).gameObject.BindEvent(_ =>
        {
            Managers.Sound.PlaySFX(0);
            PlayerPrefs.SetString("nextScene", Managers.Scene.GetSceneName(Define.Scene.SelectCharacter));
            Managers.Scene.LoadScene(Define.Scene.Loading);
        });
        // 게임 종료 (에디터 플레이 중엔 에디터 재생 정지, 빌드 시엔 애플리케이션 종료)
        GetButton((int)Buttons.ExitGame).gameObject.BindEvent(_ =>
        {
#if UNITY_EDITOR
            Managers.Sound.PlaySFX(0);
            EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        });
    }

    void Update()
    {
        // ESC 키 처리: 설정 팝업 닫기 또는 종료 확인 팝업 열기      
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            var settingsGO = Managers.UI.Root.transform.Find("SettingsUI");
            if (settingsGO != null && settingsGO.gameObject.activeSelf)
            {
                settingsGO.GetComponent<UI_Settings>().ClosePopupUI();
            }
            
        }
    }

    IEnumerator PulseTitle()
    {
        // 폰트 크기를 PingPong으로 변경하며 펄스 효과
        while (true)
        {
            float size = Mathf.PingPong(Time.time * pulseSpeed,
                                        titleMaxSize - titleMinSize)
                         + titleMinSize;
            _titleComponent.fontSize = size;
            yield return null;
        }
    }

    private void OnDestroy()
    {
        // 펄스 코루틴 종료
        if (_pulseCoroutine != null)
            StopCoroutine(_pulseCoroutine);
    }

    public void ResetData()
    {
        PlayerPrefs.DeleteAll();
    }
}