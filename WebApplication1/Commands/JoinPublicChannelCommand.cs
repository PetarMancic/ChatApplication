using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Commands;

public record JoinPublicChannelCommand(string ChannelId, string UserId) : IRequest<ChannelSummary>;
