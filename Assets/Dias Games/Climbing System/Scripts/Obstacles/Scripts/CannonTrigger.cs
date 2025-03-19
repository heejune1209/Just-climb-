using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class CannonTrigger : MonoBehaviour
{
    public List<GameObject> cannons;

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            foreach (GameObject cannon in cannons)
            {
                Cannon cannon1 = cannon.GetComponent<Cannon>();
                cannon1.StartShoot();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            foreach (GameObject cannon in cannons)
            {
                Cannon cannon1 = cannon.GetComponent<Cannon>();
                cannon1.StopShoot();
            }
        }
    }
}
