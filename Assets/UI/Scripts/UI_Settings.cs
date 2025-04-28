using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Settings 팝업 전체를 자동 바인딩하고 로직을 처리하는 스크립트
public class UI_Settings : UI_Popup
{
    enum Buttons
    {
        ResLeft,          // Resolution 좌측 화살표 버튼
        ResRight,         // Resolution 우측 화살표 버튼
        ScreenModeLeft,   // Screen Mode 좌측 화살표 버튼
        ScreenModeRight,  // Screen Mode 우측 화살표 버튼
        Apply_Button,     // Apply 버튼
        Return_Button     // 설정 팝업 닫기 버튼
    }

    enum Sliders
    {
        BackGroundMusic,  // BackGround Music 슬라이더
        EffectSound       // EffectSound 슬라이더
    }

    enum Texts
    {
        SettingsTitle,      // SettingsTitle 텍스트
        BackGroundMusicText,// BackGroundMusicText 텍스트
        EffectSoundText     // EffectSoundText 텍스트
    }

    [Header("Graphics Settings")]
    public List<ResItem> resolutions = new List<ResItem>();
    public List<string> screenModes = new List<string> { "Windowed", "Fullscreen" };
    int selectedResolution = 0;
    int selectedScreenMode = 0;

    [Header("Audio Settings")]
    public AudioMixer theMixer;

    // MenuValue 텍스트 컴포넌트
    TMP_Text _resolutionValueText;
    TMP_Text _screenModeValueText;

    private void Start()
    {
        Init();
        // Util.FindChild 비-제네릭 버전 사용
        GameObject resGO = Util.FindChild(gameObject, "Resolution", true);
        if (resGO == null)
            Debug.LogError("Resolution 오브젝트를 찾을 수 없습니다.");
        else
            _resolutionValueText = resGO.transform.Find("MenuValue").GetComponent<TextMeshProUGUI>();

        GameObject modeGO = Util.FindChild(gameObject, "ScreenMode", true);
        if (modeGO == null)
            Debug.LogError("ScreenMode 오브젝트를 찾을 수 없습니다.");
        else
            _screenModeValueText = modeGO.transform.Find("MenuValue").GetComponent<TextMeshProUGUI>();
        
        InitializeGraphics();
        InitializeAudio();
    }
    //private void Update()
    //{
    //    // ESC 키를 눌렀을 때 팝업 닫기
    //    if (Keyboard.current.escapeKey.wasPressedThisFrame)
    //    {
    //        ClosePopupUI();
    //    }
    //}

    public override void Init()
    {
        base.Init();
        Bind<Button>(typeof(Buttons));
        Bind<Slider>(typeof(Sliders));
        Bind<TextMeshProUGUI>(typeof(Texts));

        // 라벨 텍스트 설정
        GetText((int)Texts.SettingsTitle).text = "Settings";
        GetText((int)Texts.BackGroundMusicText).text = "Background Music";
        GetText((int)Texts.EffectSoundText).text = "Effect Sound";

        // 해상도 화살표 이벤트
        GetButton((int)Buttons.ResLeft).gameObject.BindEvent(_ => { /*Managers.Sound.PlaySFX(0);*/ ResLeft(); });
        GetButton((int)Buttons.ResRight).gameObject.BindEvent(_ => { /*Managers.Sound.PlaySFX(0);*/ ResRight(); });
        // 화면 모드 화살표 이벤트
        GetButton((int)Buttons.ScreenModeLeft).gameObject.BindEvent(_ => { /*Managers.Sound.PlaySFX(0);*/ ScreenModeLeft(); });
        GetButton((int)Buttons.ScreenModeRight).gameObject.BindEvent(_ => { /*Managers.Sound.PlaySFX(0);*/ ScreenModeRight(); });
        // Apply 버튼
        GetButton((int)Buttons.Apply_Button).gameObject.BindEvent(_ => { /*Managers.Sound.PlaySFX(0);*/ ApplyGraphics(); });
        // Return 버튼
        GetButton((int)Buttons.Return_Button).gameObject.BindEvent(_ => { /*Managers.Sound.PlaySFX(0);*/ ClosePopupUI(); });

        // 슬라이더 이벤트
        Get<Slider>((int)Sliders.BackGroundMusic).onValueChanged.AddListener(OnMusicVolumeChanged);
        Get<Slider>((int)Sliders.EffectSound).onValueChanged.AddListener(OnSfxVolumeChanged);
    }

    void InitializeGraphics()
    {
        // 현재 해상도 인덱스 찾기
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (Screen.width == resolutions[i].horizontal && Screen.height == resolutions[i].vertical)
            {
                selectedResolution = i; break;
            }
        }
        selectedScreenMode = Screen.fullScreen ? 1 : 0;
        UpdateGraphicsLabels();
    }

    void UpdateGraphicsLabels()
    {
        var res = resolutions[selectedResolution];
        _resolutionValueText.text = $"{res.horizontal} x {res.vertical}";
        _screenModeValueText.text = screenModes[selectedScreenMode];
    }

    public void ResLeft()
    {
        selectedResolution = Mathf.Max(0, selectedResolution - 1);
        UpdateGraphicsLabels();
    }
    public void ResRight()
    {
        selectedResolution = Mathf.Min(resolutions.Count - 1, selectedResolution + 1);
        UpdateGraphicsLabels();
    }
    public void ScreenModeLeft()
    {
        selectedScreenMode = Mathf.Max(0, selectedScreenMode - 1);
        UpdateGraphicsLabels();
    }
    public void ScreenModeRight()
    {
        selectedScreenMode = Mathf.Min(screenModes.Count - 1, selectedScreenMode + 1);
        UpdateGraphicsLabels();
    }

    public void ApplyGraphics()
    {
        Screen.fullScreen = (selectedScreenMode == 1);
        var res = resolutions[selectedResolution];
        Screen.SetResolution(res.horizontal, res.vertical, Screen.fullScreen);
        PlayerPrefs.SetInt("FullScreen", selectedScreenMode);

        // UI 갱신
        UpdateGraphicsLabels();

        Debug.Log($"Applied Resolution: {Screen.width}×{Screen.height}, FullScreen: {Screen.fullScreen}");
    }

    void InitializeAudio()
    {
        // 슬라이더 초기화
        float bgm = PlayerPrefs.GetFloat("MusicVol", 0.75f);
        Get<Slider>((int)Sliders.BackGroundMusic).value = bgm;

        float sfx = PlayerPrefs.GetFloat("SFXVol", 0.75f);
        Get<Slider>((int)Sliders.EffectSound).value = sfx;
    }

    void OnMusicVolumeChanged(float v)
    {
        theMixer.SetFloat("MusicVol", v);
        PlayerPrefs.SetFloat("MusicVol", v);
    }

    void OnSfxVolumeChanged(float v)
    {
        theMixer.SetFloat("SFXVol", v);
        PlayerPrefs.SetFloat("SFXVol", v);
    }
}

[System.Serializable]
public class ResItem { public int horizontal, vertical; }
