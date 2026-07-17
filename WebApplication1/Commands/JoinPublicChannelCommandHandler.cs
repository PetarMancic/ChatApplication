using MediatR;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Commands;

public class JoinPublicChannelCommandHandler : IRequestHandler<JoinPublicChannelCommand, ChannelSummary>
{
    private readonly IChannelStore _channelStore;

    public JoinPublicChannelCommandHandler(IChannelStore channelStore)
    {
        _channelStore = channelStore;
    }

    public async Task<ChannelSummary> Handle(JoinPublicChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _channelStore.GetByIdAsync(request.ChannelId)
            ?? throw new InvalidOperationException("Channel not found.");

        if (channel.Type != ChannelTypes.Public)
        {
            throw new InvalidOperationException("Only public channels can be joined directly.");
        }

        await _channelStore.AddMemberAsync(channel.Id, request.UserId);

        return new ChannelSummary(channel.Id, channel.Name, channel.Type, channel.Name);
    }
}
