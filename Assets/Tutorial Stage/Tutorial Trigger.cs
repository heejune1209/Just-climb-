using DiasGames.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject tutorialTrigger;



    private void OnTriggerEnter(Collider other)
    {       
        if (other.CompareTag("Player"))
        {
            Time.timeScale = 0f;
            tutorialTrigger.SetActive(true);

            
        }
    }
}
