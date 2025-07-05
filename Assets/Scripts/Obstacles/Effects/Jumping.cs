using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace JustClimb.Obstacles.Effects
{
    // 플레이어가 Trigger 영역에 닿으면 지정된 방향으로 점프시키는 컴포넌트
    [RequireComponent(typeof(Collider))]
    public class Jumping : MonoBehaviour
    {
        [Inject] private ISoundManager _soundManager;

        //public float knockbackDuration = 0.5f; 
        public float jumpForce = 5f;
        public Vector3 jumpDirection = Vector3.right;

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Rigidbody playerRigidbody = collision.gameObject.GetComponent<Rigidbody>();
                if (playerRigidbody != null)
                {
                    Vector3 knockbackVelocity = jumpDirection.normalized * jumpForce;
                    playerRigidbody.velocity = knockbackVelocity;
                    _soundManager.PlaySFX(8);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Vector3 arrowStart = transform.position;
            Vector3 arrowEnd = transform.position + jumpDirection.normalized * jumpForce;

            Gizmos.DrawWireSphere(arrowEnd, 0.1f);

            Gizmos.DrawLine(arrowStart, arrowEnd);

            Vector3 arrowHeadRight = Quaternion.Euler(0, 180 + 30, 0) * jumpDirection.normalized * 0.3f;
            Vector3 arrowHeadLeft = Quaternion.Euler(0, 180 - 30, 0) * jumpDirection.normalized * 0.3f;
            Gizmos.DrawLine(arrowEnd, arrowEnd + arrowHeadRight);
            Gizmos.DrawLine(arrowEnd, arrowEnd + arrowHeadLeft);
            Gizmos.DrawLine(arrowEnd + arrowHeadRight, arrowEnd + arrowHeadLeft);
        }

        // 메모리 누수 방지
        private void OnDestroy()
        {
            // 매니저 참조 해제
            _soundManager = null;
        }
    }

}
