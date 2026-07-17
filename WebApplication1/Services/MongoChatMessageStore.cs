using MongoDB.Driver;
using WebApplication1.Models;

namespace WebApplication1.Services;

public class MongoChatMessageStore : IChatMessageStore
{
    private readonly IMongoCollection<ChatMessage> _messages;

    public MongoChatMessageStore(IMongoDatabase database)
    {
        _messages = database.GetCollection<ChatMessage>("messages");
    }

    public Task AddAsync(ChatMessage message) => _messages.InsertOneAsync(message);

    public async Task<IReadOnlyList<ChatMessage>> GetByChannelAsync(string channelId) =>
        await _messages.Find(m => m.ChannelId == channelId)
            .SortBy(m => m.Timestamp)
            .ToListAsync();
}
