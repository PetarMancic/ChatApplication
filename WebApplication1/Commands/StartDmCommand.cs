using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Commands;

public record StartDmCommand(string RequesterUserId, string OtherUserId) : IRequest<ChannelSummary>;
