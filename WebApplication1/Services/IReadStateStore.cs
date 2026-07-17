using WebApplication1.Models;

namespace WebApplication1.Services;

public interface IReadStateStore
{
    /// <summary>Advances the user's read pointer for the channel; returns false when the
    /// stored pointer is already at or past the given message id (advance-only).</summary>
    Task<bool> UpsertIfNewerAsync(string userId, string channelId, string messageId);

    Task<IReadOnlyList<ReadState>> GetForChannelAsync(string channelId);
}
