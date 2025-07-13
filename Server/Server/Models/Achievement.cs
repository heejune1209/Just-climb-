using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Server.Models
{
    /// <summary>
    /// 업적 정의 테이블 (메타데이터)
    /// 새로운 업적 추가 시 스키마 변경 없이 데이터만 추가
    /// </summary>
    [Table("achievements")]
    public class Achievement
    {
        [Key]
        [Column("achievement_id")]
        public int AchievementId { get; set; }

        [Required]
        [StringLength(50)]
        [Column("code")]
        public string Code { get; set; } = string.Empty; // 예: "novice_climber"

        [Required]
        [StringLength(100)]
        [Column("name")]
        public string Name { get; set; } = string.Empty; // 사용자에게 보여줄 이름

        [StringLength(500)]
        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("reward_amount")]
        public int RewardAmount { get; set; } = 0; // 젬/골드 수량

        [StringLength(20)]
        [Column("reward_type")]
        public string RewardType { get; set; } = "gems"; // "gems", "gold", "item"

        [StringLength(50)]
        [Column("category")]
        public string Category { get; set; } = string.Empty; // "stage", "character", "item"

        [Column("sort_order")]
        public int SortOrder { get; set; } = 0; // UI 표시 순서

        [Column("is_active")]
        public bool IsActive { get; set; } = true; // 활성화 여부

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 사용자별 업적 상태 테이블 (실제 데이터)
    /// 정규화된 구조로 확장성과 유지보수성 확보
    /// </summary>
    [Table("user_achievements")]
    public class UserAchievement
    {
        [Required]
        [StringLength(100)]
        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("achievement_id")]
        public int AchievementId { get; set; }

        [Column("unlocked_at")]
        public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;

        [Column("claimed_at")]
        public DateTime? ClaimedAt { get; set; } // NULL = 미수령, NOT NULL = 수령완료

        // ===== FOREIGN KEYS =====
        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [ForeignKey(nameof(AchievementId))]
        public Achievement Achievement { get; set; } = null!;

        // ===== COMPOSITE PRIMARY KEY =====
        // DbContext에서 HasKey로 설정: (UserId, AchievementId)
    }
} 