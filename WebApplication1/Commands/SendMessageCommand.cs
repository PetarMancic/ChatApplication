using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Commands;

public record SendMessageCommand(string ChannelId, string User, string SenderEmail, string Message, string ClientMessageId) : IRequest<ChatMessage>;
