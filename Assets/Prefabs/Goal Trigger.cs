using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    public GameObject fire1;
    public GameObject fire2;
    public GameObject fire3;
    public GameObject fire4;
    // Start is called before the first frame update
    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            fire1.SetActive(true);
            fire2.SetActive(true);
            fire3.SetActive(true);
            fire4.SetActive(true);
            AudioManager.instance.PlaySFX(10);
            AudioManager.instance.PlaySFX(9);
        }

    }
}
