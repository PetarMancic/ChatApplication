using WebApplication1.Models;

namespace WebApplication1.Services;

public interface IUserStore
{
    Task<User?> FindByGoogleIdAsync(string googleId);
    Task<User> UpsertAsync(string googleId, string email, string name);
}
