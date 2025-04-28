using UnityEngine;
using JustClimb.Obstacles.Core;

namespace JustClimb.Obstacles.Core
{
    /// <summary>
    /// 플레이어가 트리거 영역에 진입/이탈할 때
    /// IObstacle 구현체들을 Activate/Deactivate 합니다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ObstacleTrigger : MonoBehaviour
    {
        // 자식을 포함해 이 오브젝트에 붙은 모든 IObstacle 구현체들
        private IObstacle[] _obstacles;

        private void Awake()
        {
            // Collider를 Trigger로 강제 설정
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            // 이 트리거에 속한 모든 장애물을 찾아둡니다
            _obstacles = GetComponentsInChildren<IObstacle>(includeInactive: true);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            // 플레이어가 들어오면 모두 활성화
            foreach (var obs in _obstacles)
                obs.Activate();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            // 플레이어가 나가면 모두 비활성화
            foreach (var obs in _obstacles)
                obs.Deactivate();
        }
    }
}
