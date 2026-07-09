using MediatR;
using Microsoft.AspNetCore.SignalR;
using WebApplication1.Hubs;

namespace WebApplication1.Notifications;

public class BroadcastMessageHandler : INotificationHandler<MessageSentNotification>
{
    private const string GeneralRoom = "general";

    private readonly IHubContext<ChatHub> _hubContext;

    public BroadcastMessageHandler(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task Handle(MessageSentNotification notification, CancellationToken cancellationToken)
    {
        var message = notification.Message;
        return _hubContext.Clients.Group(GeneralRoom).SendAsync(
            "ReceiveMessage", message.User, message.Message, message.Timestamp, cancellationToken);
    }
}
