using System.Collections.Generic;

namespace Server.Models
{
    /// <summary>
    /// 클라이언트에서 전송하는 델타 이벤트를 표현하는 DTO입니다.
    /// 클라이언트의 DeltaEvent와 동일한 구조
    /// </summary>
    public class DeltaEventDto
    {
        /// <summary>변경된 데이터의 식별 키</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>직렬화된 값</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>변경 발생 시각 (UTC 밀리초)</summary>
        public long Timestamp { get; set; }
    }
    /// <summary>
    /// SaveController가 요청 바디로 받는 DTO입니다.
    /// </summary>
    public class SaveRequest
    {
        /// <summary>클라이언트에서 전송된 델타 이벤트 리스트</summary>
        public List<DeltaEventDto> Deltas { get; set; } = new List<DeltaEventDto>();
    }

    /// <summary>
    /// 클라이언트의 InventoryItem과 동일한 구조로 변경
    /// </summary>
    public class InventoryItemDto
    {
        /// <summary>아이템 타입 (클라이언트에서 enum 문자열로 전송)</summary>
        public string itemId { get; set; } = string.Empty;  // ItemType enum을 문자열로 받음
        
        /// <summary>아이템 개수</summary>
        public int count { get; set; }
    }

    /// <summary>
    /// 클라이언트의 SerializableVector3와 동일한 구조
    /// </summary>
    public class SerializableVector3Dto
    {
        public float x { get; set; }
        public float y { get; set; }
        public float z { get; set; }
    }
}
