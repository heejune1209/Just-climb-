using System.Collections.Generic;
using Newtonsoft.Json;

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
    /// 클라이언트 측 InventoryItemDto와 완전히 호환
    /// </summary>
    public class InventoryItemDto
    {
        /// <summary>아이템 타입 (클라이언트에서 enum 문자열로 전송)</summary>
        [JsonProperty("itemId")]
        public string itemId { get; set; } = string.Empty;  // ItemType enum을 문자열로 받음
        
        /// <summary>아이템 개수</summary>
        [JsonProperty("count")]
        public int count { get; set; }

        // 기본 생성자
        public InventoryItemDto() { }

        // 매개변수가 있는 생성자 (클라이언트와 동일)
        public InventoryItemDto(string itemId, int count)
        {
            this.itemId = itemId;
            this.count = count;
        }
    }

    /// <summary>
    /// 클라이언트의 SerializableVector3와 동일한 구조
    /// 클라이언트 측 SerializableVector3Dto와 완전히 호환
    /// </summary>
    public class SerializableVector3Dto
    {
        [JsonProperty("x")]
        public float x { get; set; }
        
        [JsonProperty("y")]
        public float y { get; set; }
        
        [JsonProperty("z")]
        public float z { get; set; }

        // 기본 생성자
        public SerializableVector3Dto() { }

        // 매개변수가 있는 생성자 (클라이언트와 동일)
        public SerializableVector3Dto(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    // ================ 랭킹 시스템 DTO들 ================

    /// <summary>
    /// 스테이지별 랭킹 정렬 기준
    /// </summary>
    public enum RankingSortType
    {
        ClearTime = 0,    // 최단 클리어 타임
        DeathCount = 1    // 최소 사망 횟수
    }

    /// <summary>
    /// 랭킹 조회 요청 DTO
    /// </summary>
    public class RankingRequestDto
    {
        public int StageNumber { get; set; }
        public int SortType { get; set; } = 0; // RankingSortType
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// 한 스테이지의 한 명 기록
    /// </summary>
    public class RankingEntry
    {
        public int Rank { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = "Anonymous";
        public float ClearTime { get; set; }
        public int DeathCount { get; set; }
        public bool IsMyRecord { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// 랭킹 조회 응답 DTO
    /// </summary>
    public class RankingResponseDto
    {
        public int StageNumber { get; set; }
        public int SortType { get; set; }
        public List<RankingEntry> TopEntries { get; set; } = new List<RankingEntry>();
        public RankingEntry? MyEntry { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// 기록 업데이트 요청 DTO
    /// </summary>
    public class UpdateRecordRequestDto
    {
        public int StageNumber { get; set; }
        public float ClearTime { get; set; }
        public int DeathCount { get; set; }
        public string DisplayName { get; set; } = "You";
    }
}
