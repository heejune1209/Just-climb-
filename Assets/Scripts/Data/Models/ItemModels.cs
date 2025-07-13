using System;
using Newtonsoft.Json;

namespace JustClimb.Items
{
    /// <summary>
    /// 클라이언트용 아이템 모델 (기존 InventoryItem)
    /// JSON 직렬화/역직렬화 시 개별 아이템 정보를 담는 역할
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        // 아이템 고유 ID (enum-string 자동 변환)
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ItemType itemId;

        // 보유 개수
        public int count;

        public InventoryItem() { }

        public InventoryItem(ItemType itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
        }
    }
} 