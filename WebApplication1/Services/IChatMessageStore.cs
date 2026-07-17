using WebApplication1.Models;

namespace WebApplication1.Services;

public interface IChatMessageStore
{
    /// <summary>Inserts the message, or returns the already-stored one when the same
    /// (ChannelId, ClientMessageId) was inserted before (retry dedup).</summary>
    Task<(ChatMessage Message, bool Inserted)> AddOrGetAsync(ChatMessage message);

    Task<IReadOnlyList<ChatMessage>> GetByChannelAsync(string channelId);

    /// <summary>Messages in the channel with Id greater than the given one (gap recovery).</summary>
    Task<IReadOnlyList<ChatMessage>> GetAfterAsync(string channelId, string afterMessageId);
}
