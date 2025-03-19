using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Jumping : MonoBehaviour
{
    //public float knockbackDuration = 0.5f; // 넉백 지속 시간
    public float knockbackSpeed = 5f; // 넉백 속도
    public Vector3 knockbackDirection = Vector3.right; // 넉백 방향, 여기서는 초기값으로 오른쪽 방향을 사용합니다.
                                                       // (값을 1을 기준으로 부호(+,-)를 넣어보면서 방향 설정.)
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRigidbody = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                Vector3 knockbackVelocity = knockbackDirection.normalized * knockbackSpeed;
                playerRigidbody.velocity = knockbackVelocity;
                AudioManager.instance.PlaySFX(8);
                //playerRigidbody.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode.Impulse);
                // 접촉 지점이 어디든 방향 설정하면 그 방향으로 무조건 넉백.


                //Vector3 knockbackDirection = -contact.normal; // 반대 방향 벡터
                //Vector3 targetPosition = playerRigidbody.position + knockbackDirection * knockbackDistance;
                /*
                Vector3 randomKnockbackDirection = Random.insideUnitSphere.normalized; // 랜덤 방향 벡터 생성
                Vector3 targetPosition = playerRigidbody.position + randomKnockbackDirection * knockbackDistance;
                */
                //StartCoroutine(KnockbackPlayer(playerRigidbody, targetPosition, knockbackDuration));
            }
        }
    }
    /*
    // 부드러운 넉백을 위한 코루틴
    IEnumerator KnockbackPlayer(Rigidbody playerRigidbody, Vector3 targetPosition, float duration)
    {
        //Quaternion startRotation = playerRigidbody.rotation;
        //Quaternion targetRotation = Quaternion.LookRotation(knockbackDirection);

        Vector3 initialPosition = playerRigidbody.position;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration); // 보간 계수 (0~1 사이 값)
            playerRigidbody.MovePosition(Vector3.Lerp(initialPosition, targetPosition, t));
            
            yield return null;
        }
    }
    */
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 arrowStart = transform.position;
        Vector3 arrowEnd = transform.position + knockbackDirection.normalized * knockbackSpeed;

        // 화살표 꼭대기 부분
        Gizmos.DrawWireSphere(arrowEnd, 0.1f);

        // 화살표 선
        Gizmos.DrawLine(arrowStart, arrowEnd);

        // 화살표 삼각형 부분
        Vector3 arrowHeadRight = Quaternion.Euler(0, 180 + 30, 0) * knockbackDirection.normalized * 0.3f;
        Vector3 arrowHeadLeft = Quaternion.Euler(0, 180 - 30, 0) * knockbackDirection.normalized * 0.3f;
        Gizmos.DrawLine(arrowEnd, arrowEnd + arrowHeadRight);
        Gizmos.DrawLine(arrowEnd, arrowEnd + arrowHeadLeft);
        Gizmos.DrawLine(arrowEnd + arrowHeadRight, arrowEnd + arrowHeadLeft);
        // 넉백 방향을 기즈모로 그리기 (기즈모는 에디터에서 선택된 상태일 때만 보임)
    }

    // 선형보간 사용 안한 코드
    /*
    public float knockbackForce = 10f; // 넉백에 사용될 힘
    
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            Rigidbody playerRigidbody = collision.gameObject.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                ContactPoint contact = collision.GetContact(0); // 충돌 지점 가져오기

                // 충돌 지점에서 플레이어로 향하는 방향 계산
                Vector3 knockbackDirection = - contact.normal;

                // 반대 방향으로 넉백을 주고 플레이어를 특정 힘으로 밀어냄
                playerRigidbody.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
            }
        }
    }
    */
}
