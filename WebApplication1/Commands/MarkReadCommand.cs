using MediatR;

namespace WebApplication1.Commands;

/// <summary>Returns true when the pointer actually advanced (advance-only semantics).</summary>
public record MarkReadCommand(string ChannelId, string UserId, string MessageId) : IRequest<bool>;
