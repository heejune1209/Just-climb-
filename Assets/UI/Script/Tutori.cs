using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutori : MonoBehaviour
{
    public GameObject TutorialPanel;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !PlayerPrefs.HasKey("TutorialDisplayed"))
        {
            Time.timeScale = 0f;
            TutorialPanel.SetActive(true);
            PlayerPrefs.SetInt("TutorialDisplayed", 1);
            PlayerPrefs.Save();
        }
    }
}
