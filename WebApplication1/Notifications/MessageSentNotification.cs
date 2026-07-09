using MediatR;
using WebApplication1.Models;

namespace WebApplication1.Notifications;

public record MessageSentNotification(ChatMessage Message) : INotification;
