using DiasGames.Components;
using DiasGames.Controller;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadByContacts : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Health playerHealth = other.gameObject.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.Damage(playerHealth.CurrentHP); // �÷��̾� ü���� 0���� �����Ͽ� ��� ó��
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && (gameObject.CompareTag("Rolling") || gameObject.CompareTag("Rocks")))
        {
            Health playerHealth = collision.gameObject.GetComponent<Health>();
            if (playerHealth != null)
            {
                playerHealth.Damage(playerHealth.CurrentHP); // �÷��̾� ü���� 0���� �����Ͽ� ��� ó��
            }
        }
    }

}
