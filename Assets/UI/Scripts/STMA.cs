using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class STMA : MonoBehaviour
{
    public List<Button> stageButtons;
    public List<GameObject> lockImages;

    void Start()
    {
        for (int i = 1; i < stageButtons.Count; i++)
        {
            if (PlayerPrefs.GetInt("Stage" + i, 0) == 1)
            {
                Debug.Log("Stage " + i + " is unlocked");
                UnlockStage(i);
            }
        }
    }

    public void UnlockStage(int stageNumber)
    {
        stageButtons[stageNumber].interactable = true;
        lockImages[stageNumber].SetActive(false);
    }
}