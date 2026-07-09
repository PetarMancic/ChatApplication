using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Queries;

public record GetMessageHistoryQuery : IRequest<IReadOnlyList<ChatMessage>>;
