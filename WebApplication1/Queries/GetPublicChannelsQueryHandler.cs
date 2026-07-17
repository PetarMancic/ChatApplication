using MediatR;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Queries;

public class GetPublicChannelsQueryHandler : IRequestHandler<GetPublicChannelsQuery, IReadOnlyList<ChannelSummary>>
{
    private readonly IChannelStore _channelStore;

    public GetPublicChannelsQueryHandler(IChannelStore channelStore)
    {
        _channelStore = channelStore;
    }

    public async Task<IReadOnlyList<ChannelSummary>> Handle(GetPublicChannelsQuery request, CancellationToken cancellationToken)
    {
        var channels = await _channelStore.GetPublicAsync();
        var c = channels.Where(c => !c.Members.Contains(request.UserId)).ToList();

        return channels
            .Where(c => !c.Members.Contains(request.UserId))
            .Select(c => new ChannelSummary(c.Id, c.Name, c.Type, c.Name))
            .ToList();
    }
}
