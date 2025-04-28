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
    public float RollRate = 2f; // �߻� �ð� 1�ʿ� fireRate����ŭ �߻�
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
        Vector3 endPoint = gameObject.transform.position + direction.normalized * force; // �߻� ����� �߻� �Ŀ��� ���� ���� ���
        Gizmos.DrawLine(gameObject.transform.position, endPoint); // �߻� ����� �߻� �Ŀ��� ��Ÿ���� �� �׸���
    }
    void StartRoll()
    {
        GameObject rock = Instantiate(Rollingstone, gameObject.transform.position, Quaternion.identity);
        Rigidbody rockRb = rock.GetComponent<Rigidbody>();

        if (rockRb != null)
        {
            // ���� Ư�� �������� ���� ���� �о���ϴ�.
            rockRb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }

    }
}
