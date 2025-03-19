using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{    
    private Animator animator;
    public List<ResItem> resolutions = new List<ResItem>(); // 가능한 해상도 목록
    public List<string> screenModes = new List<string>() { "Windowed", "Fullscreen" };
    private int selectedScreenMode;

    public TMP_Text screenModeLabel;
    private int selectedResolution; // 현재 선택된 해상도 인덱스

    public TMP_Text resolutionsLable; // 현재 해상도를 표시하는 라벨
    public AudioMixer theMixer;  // 오디오 믹서 
    public Slider musicSlider, sfxSlider; // 음악 및 사운드 이펙트 볼륨 슬라이더
    public GameObject SettingsPanel;
    public AudioSource sfxmix;


    private void Start()
    {
        // 현재 화면 해상도가 가능한 해상도 목록에 있는지 확인 후, 있다면 해당 인덱스를 선택해 라벨을 업데이트
        bool foundRes = false;
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (Screen.width == resolutions[i].horizontal && Screen.height == resolutions[i].vertical)
            {
                foundRes = true;

                selectedResolution = i;

                UpdateResLabel();
            }
        }
        // 만약 현재 화면 해상도가 가능한 해상도 목록에 없다면, 새로운 ResItem을 생성하여 추가하고 라벨을 업데이트 

        if (!foundRes)
        {
            ResItem newRes = new ResItem();
            newRes.horizontal = Screen.width;
            newRes.vertical = Screen.height;

            resolutions.Add(newRes);
            selectedResolution = resolutions.Count - 1;

            UpdateResLabel();
        }

        float vol = 0f;

        // 오디오 믹서에서 음악 볼륨 값을 가져와 슬라이더의 값으로 설정
        theMixer.GetFloat("MusicVol", out vol);
        musicSlider.value = vol;
        theMixer.GetFloat("SFXVol", out vol);
        sfxSlider.value = vol;

        Mathf.RoundToInt(musicSlider.value - 30).ToString(); // 초기값 50
        Mathf.RoundToInt(sfxSlider.value - 30).ToString(); // 초기값 50
        
        if (Screen.fullScreen) // Fullscreen인 경우
        {
            selectedScreenMode = 1;
        }
        else // Windowed인 경우
        {
            selectedScreenMode = 0;
        }

        UpdateScreenModeLabel();
    }
    public void ResLeft()
    {
        // 선택된 해상도의 인덱스를 하나 줄이고 (해상도를 낮추고), 이 값이 0보다 작아지면 0으로 설정하여 리스트 범위 밖으로 나가지 않게 함
        selectedResolution--;
        if (selectedResolution < 0)
        {
            selectedResolution = 0;
        }

        // 변경된 해상도에 따라 화면에 표시되는 해상도 라벨을 업데이트
        UpdateResLabel();
    }
    public void ResRight()
    {
        selectedResolution++;
        if (selectedResolution > resolutions.Count - 1)
        {
            selectedResolution = resolutions.Count - 1;
        }

        UpdateResLabel();
    }
    public void ScreenModeLeft()
    {
        selectedScreenMode--;

        if (selectedScreenMode < 0)
            selectedScreenMode = 0;

        UpdateScreenModeLabel();
    }


    public void ScreenModeRight()
    {
        selectedScreenMode++;

        if (selectedScreenMode > screenModes.Count - 1)
            selectedScreenMode = screenModes.Count - 1;

        UpdateScreenModeLabel();
    }
    public void UpdateScreenModeLabel()
    {
        screenModeLabel.text = screenModes[selectedScreenMode];
    }

    public void UpdateResLabel()
    {
        // 현재 선택된 해상도(너비 x 높이)를 화면에 표시하는 함수
        resolutionsLable.text = resolutions[selectedResolution].horizontal.ToString() + " X " + resolutions[selectedResolution].vertical.ToString(); // 해상도 텍스트 표시
    }
    public void ApplyGraphics()
    {
        Screen.fullScreen = (selectedScreenMode == 1);
        Screen.SetResolution(resolutions[selectedResolution].horizontal, resolutions[selectedResolution].vertical, selectedScreenMode == 1);
        PlayerPrefs.SetInt("FullScreen", selectedScreenMode);

    }
    public void SetMusicVol() // 오디오 믹서에 익스포스 파라메터에 MusicVol로 이름 설정
    {
        Mathf.RoundToInt(musicSlider.value + 80).ToString();
        theMixer.SetFloat("MusicVol", musicSlider.value);

        PlayerPrefs.SetFloat("MusicVol", musicSlider.value);
    }

    public void SetSFXVol() // 오디오 믹서에 익스포스 파라메터에 SFXVol으로 이름 설정
    {
        Mathf.RoundToInt(sfxSlider.value + 80).ToString();
        theMixer.SetFloat("SFXVol", sfxSlider.value);

        PlayerPrefs.SetFloat("SFXVol", sfxSlider.value);

    }

    public void BuSd()
    {
        sfxmix.Play();
    }

    public void CloseSettingsUI()               // 버튼으로 로직 연결, UI닫는 메소드 호출 기능
    {
        animator = GetComponent<Animator>();
        StartCoroutine("CloseAfterDelay1");

    }    
    private IEnumerator CloseAfterDelay1()  // UI창 닫는 애니메이션 실행과 0.5초 텀 추가
    {                
        animator.SetTrigger("Close");
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
        animator.ResetTrigger("Close");             
    }
    public void OffSettings()
    {
        SettingsPanel.SetActive(false);
    }
}
[System.Serializable]  // 저장 기능
public class ResItem
{
    public int horizontal, vertical;
}
