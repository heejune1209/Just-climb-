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
        //Managers.Game.GoToLobby();
        PlayerPrefs.DeleteKey("PlayerRespawnX");
        PlayerPrefs.DeleteKey("PlayerRespawnY");
        PlayerPrefs.DeleteKey("PlayerRespawnZ");
        Managers.Sound.Clear();
        PlayerPrefs.SetString("nextScene", nextstagename);
        SceneManager.LoadScene("LoadingScene");
    }
    public void GoToLobbyScene()
    {
        Time.timeScale = 1f;
        //Managers.Game.GoToLobby();
        PlayerPrefs.DeleteKey("PlayerRespawnX");
        PlayerPrefs.DeleteKey("PlayerRespawnY");
        PlayerPrefs.DeleteKey("PlayerRespawnZ");
        Managers.Sound.Clear();
        PlayerPrefs.SetString("nextScene", "LobbyScene");
        SceneManager.LoadScene("LoadingScene");
        Managers.Sound.PlayBGM(1);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        //Managers.Game.GoToLobby();
        PlayerPrefs.DeleteKey("PlayerRespawnX");
        PlayerPrefs.DeleteKey("PlayerRespawnY");
        PlayerPrefs.DeleteKey("PlayerRespawnZ");
        Managers.Sound.Clear();
        PlayerPrefs.SetString("nextScene", "MainMenu");
        SceneManager.LoadScene("LoadingScene");
        Managers.Sound.PlayBGM(0);
    }
    public void ResetData()
    {
        PlayerPrefs.DeleteAll();
    }
    
}
