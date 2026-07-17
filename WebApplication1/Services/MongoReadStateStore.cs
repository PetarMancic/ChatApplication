using MongoDB.Driver;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class MongoReadStateStore : IReadStateStore
{
    private readonly IMongoCollection<ReadState> _readStates;

    public MongoReadStateStore(IMongoDatabase database)
    {
        _readStates = database.GetCollection<ReadState>("readStates");
        _readStates.Indexes.CreateOne(new CreateIndexModel<ReadState>(
            Builders<ReadState>.IndexKeys.Ascending(r => r.UserId).Ascending(r => r.ChannelId),
            new CreateIndexOptions { Unique = true }));
    }

    public async Task<bool> UpsertIfNewerAsync(string userId, string channelId, string messageId)
    {
        // ObjectId strings are timestamp-prefixed hex of fixed length, so lexicographic
        // comparison equals chronological comparison.
        try
        {
            var result = await _readStates.UpdateOneAsync(
                Builders<ReadState>.Filter.Eq(r => r.UserId, userId)
                    & Builders<ReadState>.Filter.Eq(r => r.ChannelId, channelId)
                    & Builders<ReadState>.Filter.Lt(r => r.LastReadMessageId, messageId),
                Builders<ReadState>.Update
                    .Set(r => r.LastReadMessageId, messageId)
                    .Set(r => r.UpdatedAt, DateTime.UtcNow),
                new UpdateOptions { IsUpsert = true });

            return result.ModifiedCount > 0 || result.UpsertedId != null;
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Upsert raced with the filter not matching an existing (already-newer) doc
            return false;
        }
    }

    public async Task<IReadOnlyList<ReadState>> GetForChannelAsync(string channelId) =>
        await _readStates.Find(r => r.ChannelId == channelId).ToListAsync();
}
