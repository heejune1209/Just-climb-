using Server.Models;

namespace Server.Services
{
    public interface IUserService
    {
        Task<User?> GetUserBySteamIdAsync(string steamId);
        Task<User?> GetOrCreateUserAsync(string steamId, string? steamDisplayName = null);
        Task<User?> CreateUserAsync(string steamId, string? steamDisplayName = null);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(string steamId);
    }
} 