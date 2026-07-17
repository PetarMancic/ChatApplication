using MongoDB.Bson;
using MongoDB.Driver;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class MongoChannelStore : IChannelStore
{
    private readonly IMongoCollection<Channel> _channels;

    public MongoChannelStore(IMongoDatabase database)
    {
        _channels = database.GetCollection<Channel>("channels");
        // Partial (not sparse) index: sparse still indexes explicit nulls, so all
        // non-DM channels (DmKey: null) would collide on the unique constraint.
        _channels.Indexes.CreateOne(new CreateIndexModel<Channel>(
            Builders<Channel>.IndexKeys.Ascending(c => c.DmKey),
            new CreateIndexOptions<Channel>
            {
                Unique = true,
                PartialFilterExpression = Builders<Channel>.Filter.Type(c => c.DmKey, BsonType.String)
            }));
    }

    public async Task<Channel> CreateAsync(Channel channel)
    {
        await _channels.InsertOneAsync(channel);
        return channel;
    }

    public Task<Channel?> GetByIdAsync(string id) =>
        _channels.Find(c => c.Id == id).FirstOrDefaultAsync()!;

    public async Task<IReadOnlyList<Channel>> GetForUserAsync(string userId) =>
        await _channels.Find(c => c.Members.Contains(userId))
            .SortBy(c => c.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<Channel>> GetPublicAsync() =>
        await _channels.Find(c => c.Type == ChannelTypes.Public)
            .SortBy(c => c.CreatedAt)
            .ToListAsync();

    public Task<Channel?> FindByDmKeyAsync(string dmKey) =>
        _channels.Find(c => c.DmKey == dmKey).FirstOrDefaultAsync()!;

    public Task AddMemberAsync(string channelId, string userId) =>
        _channels.UpdateOneAsync(
            c => c.Id == channelId,
            Builders<Channel>.Update.AddToSet(c => c.Members, userId));

    public Task<Channel?> FindByNameAndTypeAsync(string name, string type) =>
        _channels.Find(c => c.Name == name && c.Type == type).FirstOrDefaultAsync()!;
}
