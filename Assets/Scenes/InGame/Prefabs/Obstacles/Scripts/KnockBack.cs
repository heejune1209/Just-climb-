using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnockBack : MonoBehaviour
{
    public float knockbackDuration = 0.5f; // �˹� ���� �ð�
    public float knockbackDistance = 3f; // �˹� �Ÿ�
    public Vector3 knockbackDirection = Vector3.right; // �˹� ����, ���⼭�� �ʱⰪ���� ������ ������ ����մϴ�.
                                                       // (���� 1�� �������� ��ȣ(+,-)�� �־�鼭 ���� ����.)
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRigidbody = other.gameObject.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                //ContactPoint contact = other.GetContact(0);
                Vector3 targetPosition = playerRigidbody.position + knockbackDirection.normalized * knockbackDistance;
                Managers.Sound.PlaySFX(8);
                // ���� ������ ���� ���� �����ϸ� �� �������� ������ �˹�.


                //Vector3 knockbackDirection = -contact.normal; // �ݴ� ���� ����
                //Vector3 targetPosition = playerRigidbody.position + knockbackDirection * knockbackDistance;
                /*
                Vector3 randomKnockbackDirection = Random.insideUnitSphere.normalized; // ���� ���� ���� ����
                Vector3 targetPosition = playerRigidbody.position + randomKnockbackDirection * knockbackDistance;
                */
                StartCoroutine(KnockbackPlayer(playerRigidbody, targetPosition, knockbackDuration));
            }
        }
    }

    // �ε巯�� �˹��� ���� �ڷ�ƾ
    IEnumerator KnockbackPlayer(Rigidbody playerRigidbody, Vector3 targetPosition, float duration)
    {
        //Quaternion startRotation = playerRigidbody.rotation;
        //Quaternion targetRotation = Quaternion.LookRotation(knockbackDirection);

        Vector3 initialPosition = playerRigidbody.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration); // ���� ��� (0~1 ���� ��)
            playerRigidbody.MovePosition(Vector3.Lerp(initialPosition, targetPosition, t));

            yield return null;
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red; // ����� ���� ����
        Vector3 arrowStart = transform.position;
        Vector3 arrowEnd = transform.position + knockbackDirection.normalized * knockbackDistance;

        // ȭ��ǥ ����� �κ�
        Gizmos.DrawWireSphere(arrowEnd, 0.1f);

        // ȭ��ǥ ��
        Gizmos.DrawLine(arrowStart, arrowEnd);

        // ȭ��ǥ �ﰢ�� �κ�
        Vector3 arrowHeadRight = Quaternion.Euler(0, 180 + 30, 0) * knockbackDirection.normalized * 0.3f;
        Vector3 arrowHeadLeft = Quaternion.Euler(0, 180 - 30, 0) * knockbackDirection.normalized * 0.3f;
        Gizmos.DrawLine(arrowEnd, arrowEnd + arrowHeadRight);
        Gizmos.DrawLine(arrowEnd, arrowEnd + arrowHeadLeft);
        Gizmos.DrawLine(arrowEnd + arrowHeadRight, arrowEnd + arrowHeadLeft);
        // �˹� ������ ������ �׸��� (������ �����Ϳ��� ���õ� ������ ���� ����)
    }

    // �������� ��� ���� �ڵ�
    /*
    public float knockbackForce = 10f; // �˹鿡 ���� ��
    
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRigidbody = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                ContactPoint contact = collision.GetContact(0); // �浹 ���� ��������

                // �浹 �������� �÷��̾�� ���ϴ� ���� ���
                Vector3 knockbackDirection = - contact.normal;

                // �ݴ� �������� �˹��� �ְ� �÷��̾ Ư�� ������ �о
                playerRigidbody.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
            }
        }
    }
    */
}