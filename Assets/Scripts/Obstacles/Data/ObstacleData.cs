using UnityEngine;

namespace JustClimb.Obstacles.Data
{
    // 모든 장애물이 공유할 공통 데이터 정의용 ScriptableObject
    public abstract class ObstacleData : ScriptableObject
    {
        [Tooltip("장애물 고유 ID")]
        public string id;

        [Tooltip("UI 등에 표시할 이름")]
        public string displayName;

        [Tooltip("이 장애물을 다시 활성화하기 전 대기 시간(초)")]
        public float cooldown;
    }
}
