using MediatR;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Commands;

public class CreateChannelCommandHandler : IRequestHandler<CreateChannelCommand, ChannelSummary>
{
    private readonly IChannelStore _channelStore;

    public CreateChannelCommandHandler(IChannelStore channelStore)
    {
        _channelStore = channelStore;
    }

    public async Task<ChannelSummary> Handle(CreateChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _channelStore.CreateAsync(new Channel(
            Name: request.Name,
            Type: request.IsPrivate ? ChannelTypes.Private : ChannelTypes.Public,
            OwnerId: request.CreatorUserId,
            Members: new List<string> { request.CreatorUserId },
            DmKey: null,
            CreatedAt: DateTime.UtcNow));

        return new ChannelSummary(channel.Id, channel.Name, channel.Type, channel.Name);
    }
}
