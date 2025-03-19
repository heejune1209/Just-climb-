using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public GameObject WarningPanel;
    public GameObject SettingsUI;
    bool Closebtndown = false;
    private Keyboard keyboard;
    private static bool isBGMStarted = false;

    void Start()
    {
        keyboard = InputSystem.GetDevice<Keyboard>();
        if (!isBGMStarted)
        {
            AudioManager.instance.PlayBGM(0);

            isBGMStarted = true;
        }
    }

    void Update()
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (SettingsUI.activeSelf)
            {
                SettingsUI settingsUI = FindObjectOfType<SettingsUI>();
                settingsUI.CloseSettingsUI();
            }
            else
            {
                ReallyBackUI reallyBackUI = WarningPanel.GetComponent<ReallyBackUI>();
                reallyBackUI.ActiveWarningPanel();
            }
        }
    }

    public void OnClickGameStartButton()
    {

        PlayerPrefs.SetString("nextScene", "LobbyScene");
        SceneManager.LoadScene("LoadingScene");
        AudioManager.instance.PlayBGM(1);
    }
    public void OnClickCharButton()
    {

        PlayerPrefs.SetString("nextScene", "SelectCharacterScene");
        SceneManager.LoadScene("LoadingScene");
    }
    public void OnClickAJtButton()
    {

        PlayerPrefs.SetString("nextScene", "AchieveScene");
        SceneManager.LoadScene("LoadingScene");
    }

    public void OnClickQuitButton()
    {
#if UNITY_EDITOR
        AudioManager.instance.PlaySFX(0);
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
}

