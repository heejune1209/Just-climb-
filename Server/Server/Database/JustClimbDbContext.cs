using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    /// <summary>
    /// 사용자 기본 정보 엔티티 (Steam 인증 지원)
    /// </summary>
    public class User
    {
        /// <summary>Steam ID (Primary Key)</summary>
        public string Id { get; set; } = string.Empty;

        // 기본 정보
        public int Gold { get; set; }
        public int Gems { get; set; }
        public string SelectedCharacter { get; set; } = "Default";
        public bool TutorialDisplayed { get; set; }

        // Steam 프로필 정보
        public string? SteamDisplayName { get; set; }
        public string? SteamAvatarUrl { get; set; }

        // 타임스탬프
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<UserItem> Items { get; set; } = new List<UserItem>();
        public ICollection<UserStageRecord> StageRecords { get; set; } = new List<UserStageRecord>();
        public ICollection<UserAchievement> Achievements { get; set; } = new List<UserAchievement>(); // 정규화된 구조
        public UserAchievementProgress? AchievementProgress { get; set; }
    }

    /// <summary>
    /// 유저별 아이템 보유 현황
    /// </summary>
    public class UserItem
    {
        /// <summary>사용자 식별자 (Foreign Key)</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>아이템 식별자 (Composite Key)</summary>
        public string ItemId { get; set; } = string.Empty;

        /// <summary>소유 개수</summary>
        public int Count { get; set; }

        /// <summary>네비게이션 프로퍼티</summary>
        public User User { get; set; } = null!;
    }

    /// <summary>
    /// 랭킹 시스템을 위한 스테이지 기록
    /// </summary>
    public class UserStageRecord
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int StageNumber { get; set; }
        
        // 클리어 여부 및 기록
        public bool IsCleared { get; set; } = false;
        public int BestGemCount { get; set; } = 0;
        public float BestClearTime { get; set; } = float.MaxValue;
        public int BestDeathCount { get; set; } = int.MaxValue;
        
        // 현재 세션 데이터 (임시 저장용)
        public float CurrentPlayTime { get; set; } = 0f;
        public int CurrentDeathCount { get; set; } = 0;
        
        // 깃발 위치
        public float? FlagX { get; set; }
        public float? FlagY { get; set; }
        public float? FlagZ { get; set; }
        
        public string DisplayName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public User User { get; set; } = null!;
    }

    // UserAchievement 클래스는 별도 파일로 이동됨 (Models/UserAchievement.cs)

    /// <summary>
    /// 업적 진척도 (누적 통계)
    /// </summary>
    public class UserAchievementProgress
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string UserId { get; set; } = string.Empty;

        // 스테이지 관련 진척도
        public int StagesCompleted { get; set; } = 0;
        public int PerfectClears { get; set; } = 0;
        public int SpeedClears { get; set; } = 0;
        public int Chapter1PerfectStages { get; set; } = 0;

        // 아이템 관련 진척도
        public int ItemsPurchased { get; set; } = 0;
        public string UnlockedCharactersJson { get; set; } = "[]"; // List<string>을 JSON으로 저장
        public string ItemTypesUsedJson { get; set; } = "[]"; // List<string>을 JSON으로 저장

        // 현재 스테이지 임시 데이터
        public int DeathsInCurrentStage { get; set; } = 0;
        public bool UsedItemInCurrentStage { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public User User { get; set; } = null!;
    }
}

namespace Server.Database
{
    using Server.Models;

    /// <summary>
    /// 정리된 5개 핵심 테이블 DbContext
    /// </summary>
    public class JustClimbDbContext : DbContext
    {
        public JustClimbDbContext(DbContextOptions<JustClimbDbContext> options)
            : base(options)
        { }

        // 정규화된 6개 테이블
        public DbSet<User> Users { get; set; }
        public DbSet<UserItem> UserItems { get; set; }
        public DbSet<UserStageRecord> UserStageRecords { get; set; }
        public DbSet<Achievement> Achievements { get; set; } // 업적 정의 (메타데이터)
        public DbSet<UserAchievement> UserAchievements { get; set; } // 사용자별 업적 상태
        public DbSet<UserAchievementProgress> UserAchievementProgress { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. users 테이블 매핑
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).HasColumnName("id").HasMaxLength(100);
                entity.Property(u => u.Gold).HasColumnName("gold");
                entity.Property(u => u.Gems).HasColumnName("gems");
                entity.Property(u => u.SelectedCharacter).HasColumnName("selected_character").HasMaxLength(50);
                entity.Property(u => u.TutorialDisplayed).HasColumnName("tutorial_displayed");
                entity.Property(u => u.SteamDisplayName).HasColumnName("steam_display_name").HasMaxLength(100);
                entity.Property(u => u.SteamAvatarUrl).HasColumnName("steam_avatar_url").HasMaxLength(255);
                entity.Property(u => u.CreatedAt).HasColumnName("created_at");
                entity.Property(u => u.UpdatedAt).HasColumnName("updated_at");
            });

            // 2. user_items 테이블 매핑
            modelBuilder.Entity<UserItem>(entity =>
            {
                entity.ToTable("user_items");
                entity.HasKey(ui => new { ui.UserId, ui.ItemId });
                entity.Property(ui => ui.UserId).HasColumnName("user_id").HasMaxLength(100);
                entity.Property(ui => ui.ItemId).HasColumnName("item_id").HasMaxLength(50);
                entity.Property(ui => ui.Count).HasColumnName("count");
                
                // 외래키 설정
                entity.HasOne(ui => ui.User)
                      .WithMany(u => u.Items)
                      .HasForeignKey(ui => ui.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 3. user_stage_records 테이블 매핑 (통합 관리)
            modelBuilder.Entity<UserStageRecord>(entity =>
            {
                entity.ToTable("user_stage_records");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.UserId).HasColumnName("user_id").HasMaxLength(100);
                entity.Property(r => r.StageNumber).HasColumnName("stage_number");
                
                // 클리어 정보
                entity.Property(r => r.IsCleared).HasColumnName("is_cleared");
                entity.Property(r => r.BestGemCount).HasColumnName("best_gem_count");
                entity.Property(r => r.BestClearTime).HasColumnName("best_clear_time");
                entity.Property(r => r.BestDeathCount).HasColumnName("best_death_count");
                
                // 현재 세션
                entity.Property(r => r.CurrentPlayTime).HasColumnName("current_play_time");
                entity.Property(r => r.CurrentDeathCount).HasColumnName("current_death_count");
                
                // 깃발 위치
                entity.Property(r => r.FlagX).HasColumnName("flag_x");
                entity.Property(r => r.FlagY).HasColumnName("flag_y");
                entity.Property(r => r.FlagZ).HasColumnName("flag_z");
                
                entity.Property(r => r.DisplayName).HasColumnName("display_name").HasMaxLength(100);
                entity.Property(r => r.CreatedAt).HasColumnName("created_at");
                entity.Property(r => r.UpdatedAt).HasColumnName("updated_at");

                // 인덱스
                entity.HasIndex(r => new { r.UserId, r.StageNumber }).IsUnique();
                entity.HasIndex(r => new { r.StageNumber, r.BestClearTime });
                entity.HasIndex(r => new { r.StageNumber, r.BestDeathCount });
                
                // 외래키 설정
                entity.HasOne(r => r.User)
                      .WithMany(u => u.StageRecords)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 4. achievements 테이블 매핑 (업적 정의)
            modelBuilder.Entity<Achievement>(entity =>
            {
                entity.ToTable("achievements");
                entity.HasKey(a => a.AchievementId);
                entity.Property(a => a.AchievementId).HasColumnName("achievement_id");
                entity.Property(a => a.Code).HasColumnName("code").HasMaxLength(50);
                entity.Property(a => a.Name).HasColumnName("name").HasMaxLength(100);
                entity.Property(a => a.Description).HasColumnName("description").HasMaxLength(500);
                entity.Property(a => a.RewardAmount).HasColumnName("reward_amount");
                entity.Property(a => a.RewardType).HasColumnName("reward_type").HasMaxLength(20);
                entity.Property(a => a.Category).HasColumnName("category").HasMaxLength(50);
                entity.Property(a => a.SortOrder).HasColumnName("sort_order");
                entity.Property(a => a.IsActive).HasColumnName("is_active");
                entity.Property(a => a.CreatedAt).HasColumnName("created_at");
                entity.Property(a => a.UpdatedAt).HasColumnName("updated_at");

                // 유니크 인덱스
                entity.HasIndex(a => a.Code).IsUnique();
                entity.HasIndex(a => a.Category);
                entity.HasIndex(a => a.SortOrder);
            });

            // 5. user_achievements 테이블 매핑 (사용자별 업적 상태)
            modelBuilder.Entity<UserAchievement>(entity =>
            {
                entity.ToTable("user_achievements");
                entity.HasKey(ua => new { ua.UserId, ua.AchievementId }); // 복합 키
                entity.Property(ua => ua.UserId).HasColumnName("user_id").HasMaxLength(100);
                entity.Property(ua => ua.AchievementId).HasColumnName("achievement_id");
                entity.Property(ua => ua.UnlockedAt).HasColumnName("unlocked_at");
                entity.Property(ua => ua.ClaimedAt).HasColumnName("claimed_at");

                // 외래키 설정
                entity.HasOne(ua => ua.User)
                      .WithMany(u => u.Achievements)
                      .HasForeignKey(ua => ua.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ua => ua.Achievement)
                      .WithMany()
                      .HasForeignKey(ua => ua.AchievementId)
                      .OnDelete(DeleteBehavior.Cascade);

                // 인덱스
                entity.HasIndex(ua => ua.UserId);
                entity.HasIndex(ua => ua.UnlockedAt);
                entity.HasIndex(ua => new { ua.UserId, ua.ClaimedAt })
                      .HasFilter("claimed_at IS NULL") // 미수령 업적 빠른 조회
                      .HasDatabaseName("IX_UserAchievements_Unclaimed");

                // 제약조건: 보상 수령은 해제 이후에만 가능
                entity.HasCheckConstraint("CK_UserAchievements_ClaimedAfterUnlock", 
                    "claimed_at IS NULL OR claimed_at >= unlocked_at");
            });

            // 6. user_achievement_progress 테이블 매핑
            modelBuilder.Entity<UserAchievementProgress>(entity =>
            {
                entity.ToTable("user_achievement_progress");
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Id).HasColumnName("id");
                entity.Property(p => p.UserId).HasColumnName("user_id").HasMaxLength(100);
                
                // 진척도 정보
                entity.Property(p => p.StagesCompleted).HasColumnName("stages_completed");
                entity.Property(p => p.PerfectClears).HasColumnName("perfect_clears");
                entity.Property(p => p.SpeedClears).HasColumnName("speed_clears");
                entity.Property(p => p.Chapter1PerfectStages).HasColumnName("chapter1_perfect_stages");
                entity.Property(p => p.ItemsPurchased).HasColumnName("items_purchased");
                entity.Property(p => p.UnlockedCharactersJson).HasColumnName("unlocked_characters_json");
                entity.Property(p => p.ItemTypesUsedJson).HasColumnName("item_types_used_json");
                entity.Property(p => p.DeathsInCurrentStage).HasColumnName("deaths_in_current_stage");
                entity.Property(p => p.UsedItemInCurrentStage).HasColumnName("used_item_in_current_stage");
                entity.Property(p => p.CreatedAt).HasColumnName("created_at");
                entity.Property(p => p.UpdatedAt).HasColumnName("updated_at");
                
                // 유니크 인덱스
                entity.HasIndex(p => p.UserId).IsUnique();
                
                // 외래키 설정
                entity.HasOne(p => p.User)
                      .WithOne(u => u.AchievementProgress)
                      .HasForeignKey<UserAchievementProgress>(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
