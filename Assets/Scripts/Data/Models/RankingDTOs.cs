using System;
using System.Collections.Generic;

namespace JustClimb.Data
{
    /// <summary>
    /// 스테이지별 랭킹 정렬 기준 (서버와 동일)
    /// </summary>
    public enum RankingSortType
    {
        ClearTime = 0,    // 최단 클리어 타임
        DeathCount = 1    // 최소 사망 횟수
    }

    /// <summary>
    /// 한 스테이지의 한 명 기록 (서버 DTO와 동일)
    /// </summary>
    [Serializable]
    public class RankingEntry
    {
        public int Rank { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = "Anonymous";
        public float ClearTime { get; set; }
        public int DeathCount { get; set; }
        public bool IsMyRecord { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Unity 표시용 속성
        public string PlayerName => DisplayName;
    }

    /// <summary>
    /// 서버 응답 DTO
    /// </summary>
    [Serializable]
    public class RankingResponseDto
    {
        public int StageNumber { get; set; }
        public int SortType { get; set; }
        public List<RankingEntry> TopEntries { get; set; } = new();
        public RankingEntry MyEntry { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    /// <summary>
    /// 기록 업데이트 요청 DTO
    /// </summary>
    [Serializable]
    public class UpdateRecordRequestDto
    {
        public int StageNumber { get; set; }
        public float ClearTime { get; set; }
        public int DeathCount { get; set; }
        public string DisplayName { get; set; } = "You";
    }
} 