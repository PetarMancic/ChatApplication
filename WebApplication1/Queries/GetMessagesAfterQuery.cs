using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Queries;

public record GetMessagesAfterQuery(string ChannelId, string AfterMessageId) : IRequest<IReadOnlyList<ChatMessage>>;
