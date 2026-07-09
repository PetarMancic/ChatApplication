using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Commands;

public record SendMessageCommand(string User, string Message) : IRequest<ChatMessage>;
