using UnityEngine;
using System.Collections;
using JustClimb.Obstacles.Core;
using JustClimb.Obstacles.Data;

namespace JustClimb.Obstacles.Spawners
{
    [RequireComponent(typeof(ObstacleTrigger))]
    public class CannonShooter : ObstacleBase
    {
        [Header("포탄 발사 설정 데이터")]
        [Tooltip("에디터에서 할당할 CannonData SO")]
        public CannonData data;

        [Header("발사 위치")]
        [Tooltip("포탄이 생성되어 발사될 위치 Transform")]
        public Transform firePoint;

        // 반복 발사를 제어할 코루틴 핸들
        private Coroutine _shootRoutine;

        // 플레이어가 Trigger 영역에 들어왔을 때 발사 루틴을 시작
        public override void Activate()
        {
            if (_shootRoutine == null)
                _shootRoutine = StartCoroutine(ShootRoutine());
        }

        // 플레이어가 영역을 벗어나면 발사 루틴을 중지
        public override void Deactivate()
        {
            if (_shootRoutine != null)
            {
                StopCoroutine(_shootRoutine);
                _shootRoutine = null;
            }
        }

        // 데이터에 설정된 fireRate 만큼 반복 발사
        private IEnumerator ShootRoutine()
        {
            // 1초당 발사 횟수 → 대기 간격 계산
            float interval = 1f / data.fireRate;
            var wait = new WaitForSeconds(interval);

            while (true)
            {
                // 1) 포탄 생성 및 발사
                GameObject proj = Managers.Instance.Resource.Instantiate(
                    $"Prefabs/{data.projectilePrefab.name}", firePoint);
                var rb = proj.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.velocity = data.fireDirection.normalized * data.projectileSpeed;

                // 2) 폭발 이펙트 재생
                if (data.explosionPrefab != null)
                {
                    GameObject fx = Managers.Instance.Resource.Instantiate(
                        $"Prefabs/{data.explosionPrefab.name}", firePoint);
                    var ps = fx.GetComponent<ParticleSystem>();
                    if (ps != null)
                        ps.Play();
                }

                yield return wait;
            }
        }

        // Editor에서 발사 방향/거리 시각화
        private void OnDrawGizmosSelected()
        {
            if (firePoint == null || data == null) return;

            Gizmos.color = Color.red;
            Vector3 start = firePoint.position;
            Vector3 end = start + data.fireDirection.normalized * data.projectileSpeed;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.1f);
        }
    }
}
