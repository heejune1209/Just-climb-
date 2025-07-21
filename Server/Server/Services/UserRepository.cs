using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Models;
using System.Threading.Tasks;

namespace Server.Services
{
    /// <summary>
    /// 사용자 데이터 접근을 위한 Repository 클래스
    /// 모든 서비스에서 중복되는 사용자 조회 로직을 통합합니다.
    /// </summary>
    public class UserRepository
    {
        private readonly JustClimbDbContext _dbContext;

        public UserRepository(JustClimbDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 사용자 ID로 사용자 조회 (기본 정보만)
        /// </summary>
        public async Task<User?> GetUserByIdAsync(string userId)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }

        /// <summary>
        /// 사용자 ID로 사용자 조회 (아이템 포함)
        /// </summary>
        public async Task<User?> GetUserWithItemsAsync(string userId)
        {
            return await _dbContext.Users
                .Include(u => u.Items)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        /// <summary>
        /// 사용자 ID로 사용자 조회 (모든 연관 데이터 포함)
        /// </summary>
        public async Task<User?> GetUserWithAllDataAsync(string userId)
        {
            return await _dbContext.Users
                .Include(u => u.Items)
                .Include(u => u.StageRecords)
                .Include(u => u.Achievements)
                .Include(u => u.AchievementProgress)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        /// <summary>
        /// 사용자 존재 여부 확인
        /// </summary>
        public async Task<bool> UserExistsAsync(string userId)
        {
            return await _dbContext.Users.AnyAsync(u => u.Id == userId);
        }

        /// <summary>
        /// 사용자가 없으면 생성, 있으면 반환 (UPSERT)
        /// </summary>
        public async Task<User> GetOrCreateUserAsync(string userId, string? displayName = null)
        {
            var user = await GetUserByIdAsync(userId);
            if (user != null)
                return user;

            // 새 사용자 생성
            user = new User
            {
                Id = userId,
                SteamDisplayName = displayName ?? "Unknown",
                Gold = 0,
                Gems = 0,
                SelectedCharacter = "Default",
                TutorialDisplayed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }
    }
} 