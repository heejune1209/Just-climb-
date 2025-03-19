using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Rank : MonoBehaviour
{
    public TMP_Text stageInfoText;
    public Button[] stageButtons;
    public GameObject notClearedPanel;
    private Keyboard keyboard;

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < stageButtons.Length; i++)
        {
            int stageNumber = i + 1; 
            stageButtons[i].onClick.AddListener(() => ShowStageInfo(stageNumber));
        }
        keyboard = InputSystem.GetDevice<Keyboard>();
    }
    private void Update()
    {
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            PlayerPrefs.SetString("nextScene", "LobbyScene");
            SceneManager.LoadScene("LoadingScene");
        }
    }

    public void ShowStageInfo(int stageNumber)
    {
        // 클리어 시간 불러오기
        int clearTimeInSeconds = PlayerPrefs.GetInt("ClearTime" + stageNumber, int.MaxValue);

        if (clearTimeInSeconds == int.MaxValue)
        {
            notClearedPanel.SetActive(true);
            return;
        }

        int minutes = clearTimeInSeconds / 60;
        int seconds = clearTimeInSeconds % 60;

        // 죽은 횟수 불러오기
        int deathCount = PlayerPrefs.GetInt("Death" + stageNumber, 0);

        // 클리어 시간과 죽은 횟수를 화면에 보여주기
        stageInfoText.text = $"Clear Time : {minutes:D2} : {seconds:D2}  Death  : {deathCount}";
    }
}
