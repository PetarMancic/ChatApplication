using MongoDB.Driver;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class MongoUserStore : IUserStore
{
    private readonly IMongoCollection<User> _users;

    public MongoUserStore(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");
    }

    public Task<User?> FindByGoogleIdAsync(string googleId) =>
        _users.Find(u => u.GoogleId == googleId).FirstOrDefaultAsync()!;

    public async Task<User> UpsertAsync(string googleId, string email, string name)
    {
        var existing = await FindByGoogleIdAsync(googleId);
        if (existing is not null)
        {
            var updated = existing with { Email = email, Name = name };
            await _users.ReplaceOneAsync(u => u.Id == existing.Id, updated);
            return updated;
        }

        var user = new User(googleId, email, name, DateTime.UtcNow);
        await _users.InsertOneAsync(user);
        return user;
    }
}
