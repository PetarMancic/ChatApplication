using MediatR;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Queries;

public class GetMyChannelsQueryHandler : IRequestHandler<GetMyChannelsQuery, IReadOnlyList<ChannelSummary>>
{
    private readonly IChannelStore _channelStore;
    private readonly IUserStore _userStore;

    public GetMyChannelsQueryHandler(IChannelStore channelStore, IUserStore userStore)
    {
        _channelStore = channelStore;
        _userStore = userStore;
    }

    public async Task<IReadOnlyList<ChannelSummary>> Handle(GetMyChannelsQuery request, CancellationToken cancellationToken)
    {
        var channels = await _channelStore.GetForUserAsync(request.UserId);
        var summaries = new List<ChannelSummary>(channels.Count);

        foreach (var channel in channels)
        {
            var displayName = channel.Name;
            if (channel.Type == ChannelTypes.Dm)
            {
                var otherId = channel.Members.FirstOrDefault(m => m != request.UserId);
                displayName = otherId is null
                    ? "Unknown"
                    : (await _userStore.FindByIdAsync(otherId))?.Name ?? "Unknown";
            }

            summaries.Add(new ChannelSummary(channel.Id, channel.Name, channel.Type, displayName));
        }

        return summaries;
    }
}
