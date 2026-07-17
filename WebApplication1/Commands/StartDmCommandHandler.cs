using MediatR;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Commands;

public class StartDmCommandHandler : IRequestHandler<StartDmCommand, ChannelSummary>
{
    private readonly IChannelStore _channelStore;
    private readonly IUserStore _userStore;

    public StartDmCommandHandler(IChannelStore channelStore, IUserStore userStore)
    {
        _channelStore = channelStore;
        _userStore = userStore;
    }

    public async Task<ChannelSummary> Handle(StartDmCommand request, CancellationToken cancellationToken)
    {
        if (request.RequesterUserId == request.OtherUserId)
        {
            throw new InvalidOperationException("Cannot start a direct message with yourself.");
        }

        var other = await _userStore.FindByIdAsync(request.OtherUserId)
            ?? throw new InvalidOperationException("User not found.");

        var ids = new[] { request.RequesterUserId, request.OtherUserId }
            .OrderBy(x => x, StringComparer.Ordinal);
        var dmKey = string.Join(":", ids);

        var channel = await _channelStore.FindByDmKeyAsync(dmKey)
            ?? await _channelStore.CreateAsync(new Channel(
                Name: string.Empty,
                Type: ChannelTypes.Dm,
                OwnerId: request.RequesterUserId,
                Members: new List<string> { request.RequesterUserId, request.OtherUserId },
                DmKey: dmKey,
                CreatedAt: DateTime.UtcNow));

        return new ChannelSummary(channel.Id, channel.Name, channel.Type, other.Name, other.Id);
    }
}
