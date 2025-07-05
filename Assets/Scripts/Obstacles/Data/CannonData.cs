using UnityEngine;
using JustClimb.Obstacles.Data;

namespace JustClimb.Obstacles.Data
{
    // 포탄 발사 장애물(Cannon)의 설정을 담는 ScriptableObject
    [CreateAssetMenu(fileName = "CannonData", menuName = "Game/ObstacleData/Cannon")]
    public class CannonData : ObstacleData
    {
        [Tooltip("발사할 포탄 프리팹")]
        public GameObject projectilePrefab;

        [Tooltip("폭발 이펙트 프리팹")]
        public GameObject explosionPrefab;

        [Tooltip("발사 방향")]
        public Vector3 fireDirection = Vector3.forward;

        [Tooltip("포탄 속도")]
        public float projectileSpeed = 10f;
    }
}
