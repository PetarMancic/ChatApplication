using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Commands;

public record CreateChannelCommand(string Name, bool IsPrivate, string CreatorUserId) : IRequest<ChannelSummary>;
