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
    public void ActiveWarningPanel() // ��� �г��� ��Ƽ��Ǵ� ���
    {        
        gameObject.SetActive(true);
    }
    public void ActiveoptionPanel() // ��� �г��� ��Ƽ��Ǵ� ���
    {
        optionPanel.SetActive(true);
    }


    public void CloseBackUI()  // ��ư���� ���� ����
    {
        StartCoroutine("CloseAfterDelay3");

    }
    public void GoToMainScene()
    {
        Time.timeScale = 1f;
        //Managers.Game.GoToLobby();
        PlayerPrefs.DeleteKey("PlayerRespawnX");
        PlayerPrefs.DeleteKey("PlayerRespawnY");
        PlayerPrefs.DeleteKey("PlayerRespawnZ");

        PlayerPrefs.SetString("nextScene", "MainMenu");
        SceneManager.LoadScene("LoadingScene");
        Managers.Sound.PlayBGM(0);
    }
    public void GoToLobbyScene()
    {
        Time.timeScale = 1f;
        //Managers.Game.GoToLobby();
        PlayerPrefs.DeleteKey("PlayerRespawnX");
        PlayerPrefs.DeleteKey("PlayerRespawnY");
        PlayerPrefs.DeleteKey("PlayerRespawnZ");

        PlayerPrefs.SetString("nextScene", "LobbyScene");
        SceneManager.LoadScene("LoadingScene");
        Managers.Sound.PlayBGM(1);
    }
    private IEnumerator CloseAfterDelay3()  // UIâ �ݴ� �ִϸ��̼� ����� 0.5�� �� �߰�
    {
        BackUIanimator.SetTrigger("Close");
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
        BackUIanimator.ResetTrigger("Close");        
    }
}
