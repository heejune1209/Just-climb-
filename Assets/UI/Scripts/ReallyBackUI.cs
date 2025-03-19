using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReallyBackUI : MonoBehaviour
{
    private Animator BackUIanimator;
    public GameObject optionPanel;
       
    private void Awake()
    {
        BackUIanimator = GetComponent<Animator>();
    }
    public void ActiveWarningPanel() // 경고 패널이 액티브되는 기능
    {        
        gameObject.SetActive(true);
    }
    public void ActiveoptionPanel() // 경고 패널이 액티브되는 기능
    {
        optionPanel.SetActive(true);
    }


    public void CloseBackUI()  // 버튼으로 로직 연결
    {
        StartCoroutine("CloseAfterDelay3");

    }
    public void GoToMainScene()
    {
        Time.timeScale = 1f;
        GameManager.Instance.GoToLobby();
        PlayerPrefs.DeleteKey("PlayerRespawnX");
        PlayerPrefs.DeleteKey("PlayerRespawnY");
        PlayerPrefs.DeleteKey("PlayerRespawnZ");

        PlayerPrefs.SetString("nextScene", "MainMenu");
        SceneManager.LoadScene("LoadingScene");
        AudioManager.instance.PlayBGM(0);
    }
    public void GoToLobbyScene()
    {
        Time.timeScale = 1f;
        GameManager.Instance.GoToLobby();
        PlayerPrefs.DeleteKey("PlayerRespawnX");
        PlayerPrefs.DeleteKey("PlayerRespawnY");
        PlayerPrefs.DeleteKey("PlayerRespawnZ");

        PlayerPrefs.SetString("nextScene", "LobbyScene");
        SceneManager.LoadScene("LoadingScene");
        AudioManager.instance.PlayBGM(1);
    }
    private IEnumerator CloseAfterDelay3()  // UI창 닫는 애니메이션 실행과 0.5초 텀 추가
    {
        BackUIanimator.SetTrigger("Close");
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
        BackUIanimator.ResetTrigger("Close");        
    }
}
