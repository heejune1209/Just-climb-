using System.Collections;
using UnityEngine;

namespace JustClimb.Obstacles.Effects
{
    // 플레이어가 Trigger 영역에 닿으면 지정된 방향으로 넉백시키는 컴포넌트
    [RequireComponent(typeof(Collider))]
    public class KnockbackZone : MonoBehaviour
    {
        [Header("넉백 설정")]
        [Tooltip("넉백 지속 시간 (초)")]
        public float duration = 0.5f;

        [Tooltip("넉백 이동 거리")]
        public float distance = 3f;

        [Tooltip("넉백 방향 (로컬 X/Y/Z 축 기준)")]
        public Vector3 direction = Vector3.back;

        private void Reset()
        {
            // Collider를 트리거 모드로 설정
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            var rb = other.GetComponent<Rigidbody>();
            if (rb != null)
                StartCoroutine(KnockbackCoroutine(rb));
        }

        private IEnumerator KnockbackCoroutine(Rigidbody rb)
        {
            // 현재 위치와 목표 위치 계산
            Vector3 startPos = rb.position;
            Vector3 targetPos = startPos + direction.normalized * distance;
            float elapsed = 0f;

            // 잠시 동안 물리 이동을 위한 kinematic 모드 설정
            bool wasKinematic = rb.isKinematic;
            rb.isKinematic = true;

            // 선형 보간으로 넉백
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rb.MovePosition(Vector3.Lerp(startPos, targetPos, t));
                yield return null;
            }

            // 원래 모드 복원
            rb.isKinematic = wasKinematic;
        }
    }
}
