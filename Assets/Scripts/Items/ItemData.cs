using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JustClimb.Items
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Game/ItemData")]
    public class ItemData : ScriptableObject
    {
        public string itemId;       // 고유 ID
        public string displayName;  // 화면에 표시될 이름
        public Sprite icon;         // 인벤토리 아이콘

        [Header("효과 버프 지속시간(초)")]
        [Tooltip("0이면 버프 없음")]
        public float buffDuration;    // 새로 추가
    }
}

