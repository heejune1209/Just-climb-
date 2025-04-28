using UnityEngine;
using JustClimb.Obstacles.Data;

namespace JustClimb.Obstacles.Data
{
    // 구르는 돌 장애물(Roller)의 설정을 담는 ScriptableObject
    [CreateAssetMenu(fileName = "RollerData", menuName = "Game/ObstacleData/Roller")]
    public class RollerData : ObstacleData
    {
        [Tooltip("스폰할 구르는 돌 프리팹")]
        public GameObject stonePrefab;

        [Tooltip("굴러가는 방향")]
        public Vector3 direction = Vector3.forward;

        [Tooltip("굴러가는 힘 (임펄스)")]
        public float force = 10f;

        [Tooltip("굴러가는 빈도 (초당 스폰 횟수)")]
        public float rollRate = 2f;
    }
}
