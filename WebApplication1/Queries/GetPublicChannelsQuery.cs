using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Queries;

public record GetPublicChannelsQuery(string UserId) : IRequest<IReadOnlyList<ChannelSummary>>;
