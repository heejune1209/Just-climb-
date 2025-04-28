using UnityEngine;
using JustClimb.Obstacles.Core;

// 풀링 가능한 장애물의 기본 베이스.
// ObstacleBase의 Activate/Deactivate 로직에 더해,
// Deactivate 시 자동으로 풀(PoolManager)에 반환.
[RequireComponent(typeof(Poolable))]
public abstract class PoolableObstacle : ObstacleBase
{
    // 장애물이 비활성화(플레이어가 영역을 벗어남)될 때
    // 코루틴 정리 후 풀에 반환.
    public override void Deactivate()
    {
        // ObstacleBase의 StopAllCoroutines 등 기본 비활성화 로직 실행
        base.Deactivate();

        // Poolable 컴포넌트를 통해 풀에 반환
        Poolable poolable = GetComponent<Poolable>();
        Managers.Pool.Push(poolable);
    }
}
