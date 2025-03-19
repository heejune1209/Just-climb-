using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{    
    private Animator animator;
    public List<ResItem> resolutions = new List<ResItem>(); // ������ �ػ� ���
    public List<string> screenModes = new List<string>() { "Windowed", "Fullscreen" };
    private int selectedScreenMode;

    public TMP_Text screenModeLabel;
    private int selectedResolution; // ���� ���õ� �ػ� �ε���

    public TMP_Text resolutionsLable; // ���� �ػ󵵸� ǥ���ϴ� ��
    public AudioMixer theMixer;  // ����� �ͼ� 
    public Slider musicSlider, sfxSlider; // ���� �� ���� ����Ʈ ���� �����̴�
    public GameObject SettingsPanel;
    public AudioSource sfxmix;


    private void Start()
    {
        // ���� ȭ�� �ػ󵵰� ������ �ػ� ��Ͽ� �ִ��� Ȯ�� ��, �ִٸ� �ش� �ε����� ������ ���� ������Ʈ
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
        // ���� ���� ȭ�� �ػ󵵰� ������ �ػ� ��Ͽ� ���ٸ�, ���ο� ResItem�� �����Ͽ� �߰��ϰ� ���� ������Ʈ 

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

        // ����� �ͼ����� ���� ���� ���� ������ �����̴��� ������ ����
        theMixer.GetFloat("MusicVol", out vol);
        musicSlider.value = vol;
        theMixer.GetFloat("SFXVol", out vol);
        sfxSlider.value = vol;

        Mathf.RoundToInt(musicSlider.value - 30).ToString(); // �ʱⰪ 50
        Mathf.RoundToInt(sfxSlider.value - 30).ToString(); // �ʱⰪ 50
        
        if (Screen.fullScreen) // Fullscreen�� ���
        {
            selectedScreenMode = 1;
        }
        else // Windowed�� ���
        {
            selectedScreenMode = 0;
        }

        UpdateScreenModeLabel();
    }
    public void ResLeft()
    {
        // ���õ� �ػ��� �ε����� �ϳ� ���̰� (�ػ󵵸� ���߰�), �� ���� 0���� �۾����� 0���� �����Ͽ� ����Ʈ ���� ������ ������ �ʰ� ��
        selectedResolution--;
        if (selectedResolution < 0)
        {
            selectedResolution = 0;
        }

        // ����� �ػ󵵿� ���� ȭ�鿡 ǥ�õǴ� �ػ� ���� ������Ʈ
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
        // ���� ���õ� �ػ�(�ʺ� x ����)�� ȭ�鿡 ǥ���ϴ� �Լ�
        resolutionsLable.text = resolutions[selectedResolution].horizontal.ToString() + " X " + resolutions[selectedResolution].vertical.ToString(); // �ػ� �ؽ�Ʈ ǥ��
    }
    public void ApplyGraphics()
    {
        Screen.fullScreen = (selectedScreenMode == 1);
        Screen.SetResolution(resolutions[selectedResolution].horizontal, resolutions[selectedResolution].vertical, selectedScreenMode == 1);
        PlayerPrefs.SetInt("FullScreen", selectedScreenMode);

    }
    public void SetMusicVol() // ����� �ͼ��� �ͽ����� �Ķ���Ϳ� MusicVol�� �̸� ����
    {
        Mathf.RoundToInt(musicSlider.value + 80).ToString();
        theMixer.SetFloat("MusicVol", musicSlider.value);

        PlayerPrefs.SetFloat("MusicVol", musicSlider.value);
    }

    public void SetSFXVol() // ����� �ͼ��� �ͽ����� �Ķ���Ϳ� SFXVol���� �̸� ����
    {
        Mathf.RoundToInt(sfxSlider.value + 80).ToString();
        theMixer.SetFloat("SFXVol", sfxSlider.value);

        PlayerPrefs.SetFloat("SFXVol", sfxSlider.value);

    }

    public void BuSd()
    {
        sfxmix.Play();
    }

    public void CloseSettingsUI()               // ��ư���� ���� ����, UI�ݴ� �޼ҵ� ȣ�� ���
    {
        animator = GetComponent<Animator>();
        StartCoroutine("CloseAfterDelay1");

    }    
    private IEnumerator CloseAfterDelay1()  // UIâ �ݴ� �ִϸ��̼� ����� 0.5�� �� �߰�
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
[System.Serializable]  // ���� ���
public class ResItem
{
    public int horizontal, vertical;
}
