using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    /// <summary>
    /// 사용자 스테이지별 최고 기록 테이블
    /// </summary>
    [Table("user_stage_records")]
    public class UserStageRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int StageNumber { get; set; }

        /// <summary>
        /// 최단 클리어 타임 (초)
        /// </summary>
        public float BestClearTime { get; set; }

        /// <summary>
        /// 최소 사망 횟수
        /// </summary>
        public int BestDeathCount { get; set; }

        /// <summary>
        /// 플레이어 표시 이름
        /// </summary>
        [StringLength(50)]
        public string DisplayName { get; set; } = "Anonymous";

        /// <summary>
        /// 기록 갱신 시간
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 기록 생성 시간
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 복합 인덱스를 위한 설정 (UserId + StageNumber 조합은 유니크)
        // EF Core에서 Fluent API로 설정
    }

    /// <summary>
    /// 랭킹 조회 시 사용할 DTO
    /// </summary>
    public class RankingEntryDto
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
    /// 랭킹 조회 요청 DTO
    /// </summary>
    public class RankingRequestDto
    {
        public int StageNumber { get; set; } = 1;
        public RankingSortType SortType { get; set; } = RankingSortType.ClearTime;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// 랭킹 조회 응답 DTO
    /// </summary>
    public class RankingResponseDto
    {
        public int StageNumber { get; set; }
        public RankingSortType SortType { get; set; }
        public List<RankingEntryDto> TopEntries { get; set; } = new();
        public RankingEntryDto? MyEntry { get; set; }
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
        public string DisplayName { get; set; } = "Anonymous";
    }

    /// <summary>
    /// 랭킹 정렬 기준 (클라이언트와 동일)
    /// </summary>
    public enum RankingSortType
    {
        ClearTime = 0,    // 최단 클리어 타임
        DeathCount = 1    // 최소 사망 횟수
    }
} 