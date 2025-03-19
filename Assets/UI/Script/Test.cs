using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Test : MonoBehaviour
{
    
    [SerializeField]
    private string nextstagename;
    public void GoToShopScene()
    {
        SceneManager.LoadScene("Shop");
    }

    public void GoToNextStage()
    {
        Time.timeScale = 1f;
        GameManager.Instance.GoToLobby();
        PlayerPrefs.DeleteKey("PlayerRespawnX");
        PlayerPrefs.DeleteKey("PlayerRespawnY");
        PlayerPrefs.DeleteKey("PlayerRespawnZ");
        AudioManager.instance.Destroy();
        PlayerPrefs.SetString("nextScene", nextstagename);
        SceneManager.LoadScene("LoadingScene");
    }
    public void GoToLobbyScene()
    {
        Time.timeScale = 1f;
        GameManager.Instance.GoToLobby();
        PlayerPrefs.DeleteKey("PlayerRespawnX");
        PlayerPrefs.DeleteKey("PlayerRespawnY");
        PlayerPrefs.DeleteKey("PlayerRespawnZ");
        AudioManager.instance.Destroy();
        PlayerPrefs.SetString("nextScene", "LobbyScene");
        SceneManager.LoadScene("LoadingScene");
        AudioManager.instance.PlayBGM(1);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        GameManager.Instance.GoToLobby();
        PlayerPrefs.DeleteKey("PlayerRespawnX");
        PlayerPrefs.DeleteKey("PlayerRespawnY");
        PlayerPrefs.DeleteKey("PlayerRespawnZ");
        AudioManager.instance.Destroy();
        PlayerPrefs.SetString("nextScene", "MainMenu");
        SceneManager.LoadScene("LoadingScene");
        AudioManager.instance.PlayBGM(0);
    }
    public void ResetData()
    {
        PlayerPrefs.DeleteAll();
    }
    
}
