using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Queries;

public record GetReadStatesQuery(string ChannelId) : IRequest<IReadOnlyList<ReadState>>;
