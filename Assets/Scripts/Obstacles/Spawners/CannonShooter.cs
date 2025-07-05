using JustClimb.Obstacles.Core;
using JustClimb.Obstacles.Data;
using System.Collections;
using UnityEngine;
using Zenject;

namespace JustClimb.Obstacles.Spawners
{
    [RequireComponent(typeof(ObstacleTrigger))]
    public class CannonShooter : ObstacleBase
    {
        [Inject] private IResourceManager _resourceManager;
        [Inject] private ISoundManager _soundmanager;

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
            float interval = 1f * data.rate;
            var wait = new WaitForSeconds(interval);

            while (true)
            {
                // 1) 포탄 생성 (부모 없이)
                GameObject proj = _resourceManager.Instantiate(
                    $"Prefabs/Obstacles/{data.projectilePrefab.name}", 
                    firePoint.position, firePoint.rotation, null, data._initialpoolcount);

                // SO에서 지정한 로컬 방향(data.fireDirection)을 월드 방향으로 변환
                //  예: data.fireDirection = (0, 0, 1) 이면 firePoint.forward
                Vector3 shootDir = firePoint.TransformDirection(data.fireDirection).normalized;  // data.fireDirection 사용

                // 위치와 회전을 직접 지정
                proj.transform.SetPositionAndRotation(
                    firePoint.position,
                    Quaternion.LookRotation(shootDir)  // 발사체 오리엔테이션 설정
                );

                // 속도 부여
                if (proj.TryGetComponent(out Rigidbody rb))
                {
                    rb.velocity = shootDir * data.projectileSpeed;  // ※ data.projectileSpeed 사용
                }

                // 2) 폭발 이펙트 재생
                if (data.explosionPrefab != null)
                {
                    GameObject fx = _resourceManager.Instantiate(
                        $"Prefabs/Obstacles/{data.explosionPrefab.name}", 
                        firePoint.position, firePoint.rotation, null, 7);
                    var ps = fx.GetComponent<ParticleSystem>();
                    if (ps != null)
                        ps.Play();
                    _soundmanager.PlaySFX(6);
                }

                yield return wait;
            }
        }

        // Editor에서 발사 방향/거리 시각화
        private void OnDrawGizmos()
        {
            if (firePoint == null || data == null) return;

            Gizmos.color = Color.red;
            Vector3 start = firePoint.position;
            Vector3 dir = firePoint.TransformDirection(data.fireDirection).normalized;  // SO 방향 반영
            Vector3 end = start + dir * data.projectileSpeed;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.1f);
        }

        // 메모리 누수 방지
        protected override void OnDestroy()
        {
            // 코루틴 정리
            if (_shootRoutine != null)
            {
                StopCoroutine(_shootRoutine);
                _shootRoutine = null;
            }
            
            // 컴포넌트 참조 해제
            data = null;
            firePoint = null;
            
            // 매니저 참조 해제
            _resourceManager = null;
            _soundmanager = null;
        }
    }
}
