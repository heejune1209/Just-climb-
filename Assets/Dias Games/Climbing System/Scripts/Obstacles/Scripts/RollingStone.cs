using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine;

public class RollingStone : MonoBehaviour
{
    public GameObject Rollingstone;
    private bool isrolling = false;
    public float force;
    public Vector3 direction;
    public float RollRate = 2f; // 발사 시간 1초에 fireRate값만큼 발사
    private float RollCountdown = 0f;
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartRolling();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StopRolling();
        }
    }
    public void StartRolling()
    {
        isrolling = true;
        if (isrolling)
        {
            if (RollCountdown <= 0f)
            {
                StartRoll();
                RollCountdown = 1f / RollRate;
            }

            RollCountdown -= Time.deltaTime;
        }
    }
    void StopRolling()
    {
        isrolling = false;
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Vector3 endPoint = gameObject.transform.position + direction.normalized * force; // 발사 방향과 발사 파워를 곱한 지점 계산
        Gizmos.DrawLine(gameObject.transform.position, endPoint); // 발사 방향과 발사 파워를 나타내는 선 그리기
    }
    void StartRoll()
    {
        GameObject rock = Instantiate(Rollingstone, gameObject.transform.position, Quaternion.identity);
        Rigidbody rockRb = rock.GetComponent<Rigidbody>();

        if (rockRb != null)
        {
            // 돌을 특정 방향으로 힘을 가해 밀어냅니다.
            rockRb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }

    }
}
