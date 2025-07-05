using UnityEngine;
using DiasGames.Components;    

namespace JustClimb.Obstacles.Effects
{
    // 플레이어가 Trigger 영역(또는 충돌 영역)에 닿으면 즉시 사망시키는 컴포넌트
    // 클라이밍 홀드에 붙여서, 위험 구역을 표현할 수 있습니다.
    [RequireComponent(typeof(Collider))]
    public class DeathZone : MonoBehaviour
    {
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

            var health = other.GetComponent<Health>();
            if (health != null)
            {
                // 현재 HP만큼 데미지를 입혀 즉사 처리
                health.Damage(health.CurrentHP);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Player"))
                return;

            var health = collision.gameObject.GetComponent<Health>();
            if (health != null)
            {
                health.Damage(health.CurrentHP);
            }
        }

        // 메모리 누수 방지 (참조하는 외부 컴포넌트가 없으므로 기본 구현)
        private void OnDestroy()
        {
            // 이 클래스는 별도의 메모리 정리가 필요하지 않음
        }
    }
}
