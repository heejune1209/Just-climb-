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
                playerHealth.Damage(playerHealth.CurrentHP); // 플레이어 체력을 0으로 설정하여 사망 처리
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
                playerHealth.Damage(playerHealth.CurrentHP); // 플레이어 체력을 0으로 설정하여 사망 처리
            }
        }
    }

}
