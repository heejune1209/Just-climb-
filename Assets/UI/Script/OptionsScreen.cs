using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsScreen : MonoBehaviour
{
    public Slider volumeSlider;
    public Button prevResolutionButton;
    public Button nextResolutionButton;
    public TMP_Text resolutionText;  // 현재 해상도를 보여주는 텍스트
    public Toggle fullscreenToggle;

    private Resolution[] resolutions = new Resolution[5]
    {
    new Resolution { width = 800, height = 600 },
    new Resolution { width = 1024, height = 768 },
    new Resolution { width = 1280, height = 720 },
    new Resolution { width = 1920, height = 1080 },
    new Resolution { width = 2560, height =1440 }
     };

    private int currentResolutionIndex;

    void Start()
    {
        // 현재 화면의 해상도와 가장 가까운 설정값을 찾습니다.
        currentResolutionIndex = 3;

        UpdateUI();

        // 각 UI 요소가 변경될 때마다 해당 함수를 호출하도록 설정합니다.
        volumeSlider.onValueChanged.AddListener(SetVolume);
        prevResolutionButton.onClick.AddListener(PrevResolution);
        nextResolutionButton.onClick.AddListener(NextResoluton);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        SetAndDisplayCurrentRes();
    }

    void SetVolume(float volume)
    {
        AudioListener.volume = volume;
    }

    void PrevResolution()
    {
        currentResolutionIndex = Mathf.Max(currentResolutionIndex - 1, 0);
        SetAndDisplayCurrentRes();
    }

    void NextResoluton()
    {
        currentResolutionIndex = Mathf.Min(currentResolutionIndex + 1, resolutions.Length - 1);
        SetAndDisplayCurrentRes();
    }

    void SetAndDisplayCurrentRes()
    {
        Screen.SetResolution(resolutions[currentResolutionIndex].width, resolutions[currentResolutionIndex].height,
        Screen.fullScreen);
        UpdateUI();
    }

    void UpdateUI()
    {
        resolutionText.text = resolutions[currentResolutionIndex].width + " x " + resolutions[currentResolutionIndex].height;
        volumeSlider.value = AudioListener.volume;
        fullscreenToggle.isOn = Screen.fullScreen;
    }

    void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}