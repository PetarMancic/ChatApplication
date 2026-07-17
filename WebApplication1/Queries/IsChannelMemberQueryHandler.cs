using MediatR;
using WebApplication1.Services;

namespace WebApplication1.Queries;

public class IsChannelMemberQueryHandler : IRequestHandler<IsChannelMemberQuery, bool>
{
    private readonly IChannelStore _channelStore;

    public IsChannelMemberQueryHandler(IChannelStore channelStore)
    {
        _channelStore = channelStore;
    }

    public async Task<bool> Handle(IsChannelMemberQuery request, CancellationToken cancellationToken)
    {
        var channel = await _channelStore.GetByIdAsync(request.ChannelId);
        return channel is not null && channel.Members.Contains(request.UserId);
    }
}
