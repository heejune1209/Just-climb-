using UnityEngine;

namespace JustClimb.Obstacles.Core
{
    // 모든 장애물이 구현해야 할 공통 인터페이스.
    // 플레이어가 영역에 들어왔을 때 Activate, 나갔을 때 Deactivate가 호출.
    public interface IObstacle
    {
        // 플레이어가 장애물 영역에 들어왔을 때 실행될 로직.
        void Activate();

        // 플레이어가 장애물 영역에서 나갔을 때 실행될 로직.
        // 필요 없으면 빈 구현으로 두셔도 됩니다.
        void Deactivate();
    }
}
