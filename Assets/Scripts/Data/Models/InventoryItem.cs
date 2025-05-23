using JustClimb.Items;
using System;


// 하나의 아이템 “종류(id)”와 “보유 개수(count)”를 보관하는 간단한 데이터 부분 모델
// JSON 직렬화/역직렬화 시 개별 아이템 정보를 담는 역할
[Serializable]
public class InventoryItem
{
    // 아이템 고유 ID
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
