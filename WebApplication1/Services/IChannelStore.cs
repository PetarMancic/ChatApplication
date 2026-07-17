using WebApplication1.Models;

namespace WebApplication1.Services;

public interface IChannelStore
{
    Task<Channel> CreateAsync(Channel channel);
    Task<Channel?> GetByIdAsync(string id);
    Task<IReadOnlyList<Channel>> GetForUserAsync(string userId);
    Task<IReadOnlyList<Channel>> GetPublicAsync();
    Task<Channel?> FindByDmKeyAsync(string dmKey);
    Task AddMemberAsync(string channelId, string userId);
    Task<Channel?> FindByNameAndTypeAsync(string name, string type);
}
