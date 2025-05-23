using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JustClimb.Items
{
    public enum ItemType
    {
        Feather = 1,
        Wing = 2,
        Lamp = 3,
        Flag = 4,
        // …추가 아이템…
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "Game/ItemData")]
    public class ItemData : ScriptableObject
    {
        public ItemType itemid;          // ← 변경: 숫자 키용 enum
        public string displayName;  // 화면에 표시될 이름
        public Sprite icon;         // 인벤토리 아이콘

        [Header("정적 밸런스")]
        public int price;             // ← 추가: 구매 가격
        [TextArea] public string description; // ← 추가: 설명 텍스트

        [Header("효과 버프 지속시간(초)")]
        [Tooltip("0이면 버프 없음")]
        public float buffDuration;    // 새로 추가
        [Tooltip("0이면 즉시 재사용 가능")]
        public float cooldownDuration;   // 쿨타임(초)
    }
}

