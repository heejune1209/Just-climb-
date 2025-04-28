using UnityEngine;
using JustClimb.Obstacles.Data;

namespace JustClimb.Obstacles.Data
{
    // 낙석 장애물(드로퍼)의 설정을 담는 ScriptableObject
    [CreateAssetMenu(fileName = "DropperData", menuName = "Game/ObstacleData/Dropper")]
    public class DropperData : ObstacleData
    {
        [Tooltip("스폰할 바위 프리팹")]
        public GameObject rockPrefab;

        [Tooltip("경고등이 켜져 있는 시간 (초)")]
        public float warnTime = 1f;

        [Tooltip("바위가 떨어지는 간격 (초)")]
        public float dropInterval = 2f;
    }
}
