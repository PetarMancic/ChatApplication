using MongoDB.Bson;
using MongoDB.Driver;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class MongoChatMessageStore : IChatMessageStore
{
    private readonly IMongoCollection<ChatMessage> _messages;

    public MongoChatMessageStore(IMongoDatabase database)
    {
        _messages = database.GetCollection<ChatMessage>("messages");
        // Partial (not sparse) unique index: only messages that carry a ClientMessageId
        // participate, so pre-M6 documents with null do not collide.
        _messages.Indexes.CreateOne(new CreateIndexModel<ChatMessage>(
            Builders<ChatMessage>.IndexKeys
                .Ascending(m => m.ChannelId)
                .Ascending(m => m.ClientMessageId),
            new CreateIndexOptions<ChatMessage>
            {
                Unique = true,
                PartialFilterExpression = Builders<ChatMessage>.Filter.Type(m => m.ClientMessageId, BsonType.String)
            }));
    }

    public async Task<(ChatMessage Message, bool Inserted)> AddOrGetAsync(ChatMessage message)
    {
        try
        {
            await _messages.InsertOneAsync(message);
            return (message, true);
        }
        catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            var existing = await _messages
                .Find(m => m.ChannelId == message.ChannelId && m.ClientMessageId == message.ClientMessageId)
                .FirstAsync();
            return (existing, false);
        }
    }

    public async Task<IReadOnlyList<ChatMessage>> GetByChannelAsync(string channelId) =>
        await _messages.Find(m => m.ChannelId == channelId)
            .SortBy(m => m.Timestamp)
            .ToListAsync();

    public async Task<IReadOnlyList<ChatMessage>> GetAfterAsync(string channelId, string afterMessageId) =>
        await _messages.Find(
                Builders<ChatMessage>.Filter.Eq(m => m.ChannelId, channelId)
                & Builders<ChatMessage>.Filter.Gt(m => m.Id, afterMessageId))
            .SortBy(m => m.Id)
            .ToListAsync();
}
