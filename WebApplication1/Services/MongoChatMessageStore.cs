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

    public async Task<IReadOnlyList<ChatMessage>> GetAllAsync() =>
        await _messages.Find(FilterDefinition<ChatMessage>.Empty)
            .SortBy(m => m.Timestamp)
            .ToListAsync();
}
