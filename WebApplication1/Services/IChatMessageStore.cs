using WebApplication1.Models;

namespace WebApplication1.Services;

public interface IChatMessageStore
{
    Task AddAsync(ChatMessage message);
    Task<IReadOnlyList<ChatMessage>> GetByChannelAsync(string channelId);
}
