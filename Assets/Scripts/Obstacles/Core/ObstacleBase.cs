using UnityEngine;
using JustClimb.Obstacles.Core;

namespace JustClimb.Obstacles.Core
{
    // 모든 장애물의 기본 베이스 클래스.
    // Collider를 자동으로 Trigger로 설정하고,
    // 플레이어가 진입/이탈할 때 Activate/Deactivate를 호출.
    [RequireComponent(typeof(Collider))]
    public abstract class ObstacleBase : MonoBehaviour, IObstacle
    {
        // 에디터에서 Collider를 추가하면 자동으로 isTrigger를 켜줌.
        private void Reset()
        {
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        // 플레이어가 영역에 들어왔을 때 Activate 호출
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Activate();
            }
        }

        // 플레이어가 영역에서 나갔을 때 Deactivate 호출
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                Deactivate();
            }
        }

        // 장애물이 활성화될 때 실행할 로직을 구현.
        // ex) 낙석 시작, 포탑 발사 시작 등
        public abstract void Activate();

        // 장애물이 비활성화될 때 실행할 로직을 구현.
        // 필요 없으면 빈 구현으로 두면 됨.
        // ex) 반복 호출 정지, 효과 비활성화 등
        public virtual void Deactivate()
        {
            // 기본 동작 없음
        }
    }
}
