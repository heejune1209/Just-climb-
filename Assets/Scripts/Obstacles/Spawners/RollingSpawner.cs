using JustClimb.Obstacles.Core;
using JustClimb.Obstacles.Data;
using System.Collections;
using UnityEngine;
using Zenject;

namespace JustClimb.Obstacles.Spawners
{
    [RequireComponent(typeof(ObstacleTrigger))]
    public class RollingSpawner : ObstacleBase
    {
        [Inject] private IResourceManager _resourceManager;

        [Header("Roller 설정 데이터")]
        [Tooltip("에디터에서 할당할 RollerData SO")]
        public RollerData data;

        [Header("스폰 위치")]
        [Tooltip("굴러 나올 돌이 생성될 위치 Transform")]
        public Transform spawnPoint;

        // 반복 생성 코루틴 핸들
        private Coroutine _rollRoutine;

        // 플레이어 진입 시 호출되어 생성 루틴 시작
        public override void Activate()
        {
            if (_rollRoutine == null)
                _rollRoutine = StartCoroutine(RollRoutine());
        }

        // 플레이어 이탈 시 호출되어 생성 루틴 중지
        public override void Deactivate()
        {
            if (_rollRoutine != null)
            {
                StopCoroutine(_rollRoutine);
                _rollRoutine = null;
            }
        }

        // 지정된 빈도로 돌을 생성하고 힘을 가하는 코루틴
        private IEnumerator RollRoutine()
        {
            // 1초당 rollRate 회 생성 → 간격 계산
            float interval = 1f * data.rate;
            WaitForSeconds wait = new WaitForSeconds(interval);

            while (true)
            {
                // 1) 풀링 지원 리소스 매니저로 프리팹 인스턴스화
                var stone = _resourceManager.Instantiate(
                    $"Prefabs/Obstacles/{data.stonePrefab.name}",
                    spawnPoint.position, spawnPoint.rotation, null, data._initialpoolcount);

                // 2) Rigidbody에 임펄스 추가
                Rigidbody rb = stone.GetComponent<Rigidbody>();
                if (rb != null)
                    rb.AddForce(data.direction.normalized * data.force, ForceMode.Impulse);

                yield return wait;
            }
        }

        // 씬 뷰에서 방향·힘 시각화
        private void OnDrawGizmos()
        {
            if (spawnPoint == null || data == null) return;
            Gizmos.color = Color.cyan;
            Vector3 start = spawnPoint.position;
            Vector3 end = start + data.direction.normalized * data.force;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.1f);
        }

        // 메모리 누수 방지
        protected override void OnDestroy()
        {
            // 코루틴 정리
            if (_rollRoutine != null)
            {
                StopCoroutine(_rollRoutine);
                _rollRoutine = null;
            }
            
            // 컴포넌트 참조 해제
            data = null;
            spawnPoint = null;
            
            // 매니저 참조 해제
            _resourceManager = null;
        }
    }
}
