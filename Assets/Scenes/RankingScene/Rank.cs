using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Rank : UI_Popup
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
            PlayerPrefs.SetString("nextScene", "Lobby");
            SceneManager.LoadScene("Loading");
        }
    }

    public void ShowStageInfo(int stageNumber)
    {
        // Ŭ���� �ð� �ҷ�����
        int clearTimeInSeconds = PlayerPrefs.GetInt("ClearTime" + stageNumber, int.MaxValue);

        if (clearTimeInSeconds == int.MaxValue)
        {
            notClearedPanel.SetActive(true);
            return;
        }

        int minutes = clearTimeInSeconds / 60;
        int seconds = clearTimeInSeconds % 60;

        // ���� Ƚ�� �ҷ�����
        int deathCount = PlayerPrefs.GetInt("Death" + stageNumber, 0);

        // Ŭ���� �ð��� ���� Ƚ���� ȭ�鿡 �����ֱ�
        stageInfoText.text = $"Clear Time : {minutes:D2} : {seconds:D2}  Death  : {deathCount}";
    }
}
