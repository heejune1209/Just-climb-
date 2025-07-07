using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Models;

namespace Server.Services
{
    public class UserService : IUserService
    {
        private readonly JustClimbDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(JustClimbDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> GetUserBySteamIdAsync(string steamId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == steamId);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by Steam ID: {SteamId}", steamId);
                return null;
            }
        }

        public async Task<User?> GetOrCreateUserAsync(string steamId, string? steamDisplayName = null)
        {
            try
            {
                // 기존 사용자 조회
                var existingUser = await GetUserBySteamIdAsync(steamId);
                if (existingUser != null)
                {
                    _logger.LogInformation("Found existing user for Steam ID: {SteamId}", steamId);
                    
                    // Steam 닉네임이 제공되었고 기존 닉네임과 다르면 업데이트
                    if (!string.IsNullOrEmpty(steamDisplayName) && 
                        existingUser.SteamDisplayName != steamDisplayName)
                    {
                        existingUser.SteamDisplayName = steamDisplayName;
                        existingUser.UpdatedAt = DateTime.UtcNow;
                        _context.Users.Update(existingUser);
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Updated Steam display name for user {SteamId}: {DisplayName}", 
                            steamId, steamDisplayName);
                    }
                    
                    return existingUser;
                }

                // 새 사용자 생성
                var newUser = await CreateUserAsync(steamId, steamDisplayName);
                if (newUser != null)
                {
                    _logger.LogInformation("Created new user for Steam ID: {SteamId}, DisplayName: {DisplayName}", 
                        steamId, steamDisplayName);
                    return newUser;
                }

                _logger.LogError("Failed to create new user for Steam ID: {SteamId}", steamId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrCreateUserAsync for Steam ID: {SteamId}", steamId);
                return null;
            }
        }

        public async Task<User?> CreateUserAsync(string steamId, string? steamDisplayName = null)
        {
            try
            {
                var newUser = new User
                {
                    Id = steamId,  // Steam ID를 직접 User ID로 사용
                    Gold = 0,
                    Gems = 0,
                    SelectedCharacter = "Default",
                    SteamDisplayName = steamDisplayName,  // Steam 닉네임 설정
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully created user with Steam ID: {SteamId}, DisplayName: {DisplayName}", 
                    steamId, steamDisplayName);
                
                return newUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user for Steam ID: {SteamId}", steamId);
                return null;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully updated user with ID: {Id}", user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {Id}", user.Id);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(string steamId)
        {
            try
            {
                var user = await GetUserBySteamIdAsync(steamId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for deletion with Steam ID: {SteamId}", steamId);
                    return false;
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted user with Steam ID: {SteamId}", steamId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with Steam ID: {SteamId}", steamId);
                return false;
            }
        }
    }
} 