using System;
using UnityEngine;
using Newtonsoft.Json;
using JustClimb.Items;

namespace JustClimb.Data
{
    /// <summary>
    /// 서버 호환용 InventoryItem DTO
    /// 서버 측 InventoryItemDto와 완전히 동일한 구조
    /// </summary>
    [Serializable]
    public class InventoryItemDto
    {
        [JsonProperty("itemId")]
        public string itemId = string.Empty;  // ItemType enum을 문자열로 변환
        
        [JsonProperty("count")]
        public int count = 0;
        
        public InventoryItemDto() { }
        
        public InventoryItemDto(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
        }
        
        // InventoryItem과의 호환성을 위한 변환 메서드
        public InventoryItemDto(InventoryItem item)
        {
            this.itemId = item.itemId.ToString();
            this.count = item.count;
        }
        
        // InventoryItem으로 변환
        public InventoryItem ToInventoryItem()
        {
            if (Enum.TryParse<ItemType>(itemId, out var itemType))
            {
                return new InventoryItem(itemType, count);
            }
            return new InventoryItem();
        }
    }

    /// <summary>
    /// 서버 호환용 SerializableVector3 DTO
    /// 서버 측 SerializableVector3Dto와 완전히 동일한 구조
    /// </summary>
    [Serializable]
    public class SerializableVector3Dto
    {
        [JsonProperty("x")]
        public float x = 0f;
        
        [JsonProperty("y")]
        public float y = 0f;
        
        [JsonProperty("z")]
        public float z = 0f;
        
        public SerializableVector3Dto() { }
        
        public SerializableVector3Dto(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        
        // SerializableVector3과의 호환성을 위한 변환 메서드
        public SerializableVector3Dto(SerializableVector3 vector)
        {
            this.x = vector.x;
            this.y = vector.y;
            this.z = vector.z;
        }
        
        // SerializableVector3로 변환
        public SerializableVector3 ToSerializableVector3()
        {
            return new SerializableVector3(x, y, z);
        }
        
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    /// <summary>
    /// 기존 Unity용 Vector3 직렬화 구조체 (레거시 지원)
    /// </summary>
    [Serializable]
    public struct SerializableVector3
    {
        public float x, y, z;
        
        public SerializableVector3(float x, float y, float z)
        {
            this.x = x; 
            this.y = y; 
            this.z = z;
        }
        
        public Vector3 ToVector3() => new Vector3(x, y, z);
    }
} 