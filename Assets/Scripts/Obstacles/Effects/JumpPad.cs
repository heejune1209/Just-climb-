using UnityEngine;

namespace JustClimb.Obstacles.Effects
{
    // 플레이어가 Trigger 영역에 닿으면 지정된 방향으로 점프시키는 컴포넌트
    [RequireComponent(typeof(Collider))]
    public class JumpPad : MonoBehaviour
    {
        [Header("점프 패드 설정")]
        [Tooltip("점프에 사용할 임펄스 세기")]
        public float jumpForce = 10f;

        [Tooltip("점프 방향 (기본: 위쪽)")]
        public Vector3 jumpDirection = Vector3.up;

        private void Reset()
        {
            // Collider를 Trigger 모드로 설정
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // 기존 속도 초기화 후 임펄스 적용
                rb.velocity = Vector3.zero;
                rb.AddForce(jumpDirection.normalized * jumpForce, ForceMode.Impulse);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 씬 뷰에서 점프 방향 시각화
            Gizmos.color = Color.green;
            Vector3 start = transform.position;
            Vector3 end = start + jumpDirection.normalized * jumpForce;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.1f);
        }
    }
}
