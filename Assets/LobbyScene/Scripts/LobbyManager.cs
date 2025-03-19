using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public GameObject Update_panel;
    



    /*
    void Update()
    {
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Stage1");
        }
    }
    */

    

    public void GoToMainScene()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetString("nextScene", "MainMenu");
        SceneManager.LoadScene("LoadingScene");
        AudioManager.instance.PlayBGM(0);
    }
    public void Update_panelClose()
    {
        if (Update_panel != null)
        {
            Update_panel.SetActive(false);
        }
    }
    
}

