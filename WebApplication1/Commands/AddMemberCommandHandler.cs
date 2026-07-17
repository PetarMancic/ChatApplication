using MediatR;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Commands;

public class AddMemberCommandHandler : IRequestHandler<AddMemberCommand>
{
    private readonly IChannelStore _channelStore;
    private readonly IUserStore _userStore;

    public AddMemberCommandHandler(IChannelStore channelStore, IUserStore userStore)
    {
        _channelStore = channelStore;
        _userStore = userStore;
    }

    public async Task Handle(AddMemberCommand request, CancellationToken cancellationToken)
    {
        var channel = await _channelStore.GetByIdAsync(request.ChannelId)
            ?? throw new InvalidOperationException("Channel not found.");

        if (channel.Type != ChannelTypes.Private)
        {
            throw new InvalidOperationException("Members can only be added to private channels.");
        }

        if (channel.OwnerId != request.RequesterUserId)
        {
            throw new UnauthorizedAccessException("Only the channel owner can add members.");
        }

        _ = await _userStore.FindByIdAsync(request.MemberUserId)
            ?? throw new InvalidOperationException("User not found.");

        await _channelStore.AddMemberAsync(channel.Id, request.MemberUserId);
    }
}
