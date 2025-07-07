using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Server.Models
{
    /// <summary>
    /// 사용자 상태를 표현하는 엔티티입니다. (Steam 인증 지원)
    /// </summary>
    public class User
    {
        /// <summary>Steam ID (Primary Key)</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>아이템 목록</summary>
        public ICollection<UserItem> Items { get; set; } = new List<UserItem>();

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

        // JSON 직렬화된 리스트를 저장할 컬럼들
        public string StageClearsJson { get; set; } = "[]";
        public string StageFlagPositionsJson { get; set; } = "[]";
        public string BestGemRewardsJson { get; set; } = "[]";
        public string BestClearTimesJson { get; set; } = "[]";      // 개인 기록 동기화용  
        public string BestDeathCountsJson { get; set; } = "[]";     // 개인 기록 동기화용
        
        // ✅ 유지되는 필드들 (플레이 중 임시 저장용)
        public string CurrentPlayTimesJson { get; set; } = "[]";
        public string CurrentDeathCountsJson { get; set; } = "[]";

        // 버전 관리용 (업데이트)
        public int Version { get; set; } = 2;
    }

    /// <summary>
    /// 유저별 아이템 정보를 표현하는 엔티티입니다.
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
}

namespace Server.Database
{
    using Server.Models;
    using System.Reflection.Emit;

    /// <summary>
    /// EF Core DbContext: users 및 user_items 테이블을 매핑.
    /// </summary>
    public class JustClimbDbContext : DbContext
    {
        public JustClimbDbContext(DbContextOptions<JustClimbDbContext> options)
            : base(options)
        { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserItem> UserItems { get; set; }
        public DbSet<UserStageRecord> UserStageRecords { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // users 테이블 매핑
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Id).HasColumnName("id").HasMaxLength(50);
                entity.Property(u => u.Gold).HasColumnName("gold");
                entity.Property(u => u.Gems).HasColumnName("gems");
                entity.Property(u => u.SelectedCharacter).HasColumnName("selected_character");
                entity.Property(u => u.TutorialDisplayed).HasColumnName("tutorial_displayed");
                entity.Property(u => u.SteamDisplayName).HasColumnName("steam_display_name").HasMaxLength(100);
                entity.Property(u => u.SteamAvatarUrl).HasColumnName("steam_avatar_url").HasMaxLength(255);
                entity.Property(u => u.CreatedAt).HasColumnName("created_at");
                entity.Property(u => u.UpdatedAt).HasColumnName("updated_at");
                entity.Property(u => u.StageClearsJson).HasColumnName("stage_clears_json");
                entity.Property(u => u.StageFlagPositionsJson).HasColumnName("stage_flag_positions_json");
                entity.Property(u => u.BestGemRewardsJson).HasColumnName("best_gem_rewards_json");
                entity.Property(u => u.BestClearTimesJson).HasColumnName("best_clear_times_json");
                entity.Property(u => u.BestDeathCountsJson).HasColumnName("best_death_counts_json");
                
                // ✅ 유지되는 컬럼 매핑들 (current 관련)
                entity.Property(u => u.CurrentPlayTimesJson).HasColumnName("current_play_times_json");
                entity.Property(u => u.CurrentDeathCountsJson).HasColumnName("current_death_counts_json");
                entity.Property(u => u.Version).HasColumnName("version");
            });

            // user_items 테이블 매핑
            modelBuilder.Entity<UserItem>(entity =>
            {
                entity.ToTable("user_items");
                entity.HasKey(ui => new { ui.UserId, ui.ItemId });
                entity.Property(ui => ui.UserId).HasColumnName("user_id");
                entity.Property(ui => ui.ItemId).HasColumnName("item_id");
                entity.Property(ui => ui.Count).HasColumnName("count");
                entity.HasOne(ui => ui.User)
                      .WithMany(u => u.Items)
                      .HasForeignKey(ui => ui.UserId);
            });

            // user_stage_records 테이블 매핑
            modelBuilder.Entity<UserStageRecord>(entity =>
            {
                entity.ToTable("user_stage_records");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Id).HasColumnName("id");
                entity.Property(r => r.UserId).HasColumnName("user_id");
                entity.Property(r => r.StageNumber).HasColumnName("stage_number");
                entity.Property(r => r.BestClearTime).HasColumnName("best_clear_time");
                entity.Property(r => r.BestDeathCount).HasColumnName("best_death_count");
                entity.Property(r => r.DisplayName).HasColumnName("display_name");
                entity.Property(r => r.UpdatedAt).HasColumnName("updated_at");
                entity.Property(r => r.CreatedAt).HasColumnName("created_at");

                // 복합 유니크 인덱스 (UserId + StageNumber)
                entity.HasIndex(r => new { r.UserId, r.StageNumber })
                      .IsUnique()
                      .HasDatabaseName("IX_UserStageRecords_UserId_StageNumber");

                // 랭킹 조회용 인덱스들
                entity.HasIndex(r => new { r.StageNumber, r.BestClearTime })
                      .HasDatabaseName("IX_UserStageRecords_StageNumber_BestClearTime");
                
                entity.HasIndex(r => new { r.StageNumber, r.BestDeathCount })
                      .HasDatabaseName("IX_UserStageRecords_StageNumber_BestDeathCount");
            });
        }
    }
}
