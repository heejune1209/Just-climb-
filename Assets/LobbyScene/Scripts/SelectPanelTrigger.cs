using Calcatz.JungleThemeGUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DiasGames.Controller;
using UnityEngine.InputSystem;

public class SelectPanelTrigger : MonoBehaviour
{    
    public GameObject targetTrigger;
    public End endscripts;


    private void OnTriggerEnter(Collider other)
    {
        
        if (other.CompareTag("Player"))
        {
            targetTrigger.SetActive(true);
            if(targetTrigger.tag == "Result")
            {
                Time.timeScale = 0f;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                endscripts.StageClear();
            }
        }
            
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            targetTrigger.SetActive(false);
            if (targetTrigger.tag == "Result")
            {
                targetTrigger.SetActive(true);
            }
        }
            
    }
}
