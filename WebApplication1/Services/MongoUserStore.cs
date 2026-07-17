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

    public Task<User?> FindByIdAsync(string id) =>
        _users.Find(u => u.Id == id).FirstOrDefaultAsync()!;

    public async Task<IReadOnlyList<User>> SearchAsync(string query, string excludeUserId, int limit = 20)
    {
        var regex = new MongoDB.Bson.BsonRegularExpression(
            System.Text.RegularExpressions.Regex.Escape(query), "i");
        var filter = Builders<User>.Filter.And(
            Builders<User>.Filter.Ne(u => u.Id, excludeUserId),
            Builders<User>.Filter.Or(
                Builders<User>.Filter.Regex(u => u.Name, regex),
                Builders<User>.Filter.Regex(u => u.Email, regex)));

        return await _users.Find(filter).Limit(limit).ToListAsync();
    }

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
