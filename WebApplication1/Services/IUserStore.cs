using WebApplication1.Models;

namespace WebApplication1.Services;

public interface IUserStore
{
    Task<User?> FindByGoogleIdAsync(string googleId);
    Task<User?> FindByIdAsync(string id);
    Task<IReadOnlyList<User>> SearchAsync(string query, string excludeUserId, int limit = 20);
    Task<User> UpsertAsync(string googleId, string email, string name);
}
