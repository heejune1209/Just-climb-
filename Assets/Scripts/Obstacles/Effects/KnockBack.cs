using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace JustClimb.Obstacles.Effects
{
    // 플레이어가 Trigger 영역에 닿으면 지정된 방향으로 넉백시키는 컴포넌트
    [RequireComponent(typeof(Collider))]
    public class KnockBack : MonoBehaviour
    {
        [Inject] private ISoundManager _soundManager;

        public float knockbackDuration = 0.5f;
        public float knockbackDistance = 3f;
        public Vector3 knockbackDirection = Vector3.right;

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Player"))
            {
                Rigidbody playerRigidbody = other.gameObject.GetComponent<Rigidbody>();
                if (playerRigidbody != null)
                {
                    Vector3 targetPosition = playerRigidbody.position + knockbackDirection.normalized * knockbackDistance;
                    _soundManager.PlaySFX(8);
                    StartCoroutine(KnockbackPlayer(playerRigidbody, targetPosition, knockbackDuration));
                }
            }
        }

        IEnumerator KnockbackPlayer(Rigidbody playerRigidbody, Vector3 targetPosition, float duration)
        {

            Vector3 initialPosition = playerRigidbody.position;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;

                float t = Mathf.Clamp01(elapsedTime / duration);
                playerRigidbody.MovePosition(Vector3.Lerp(initialPosition, targetPosition, t));

                yield return null;
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
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
        }

        // 메모리 누수 방지
        private void OnDestroy()
        {
            // 코루틴 정리
            StopAllCoroutines();
            
            // 매니저 참조 해제
            _soundManager = null;
        }
    }
}
