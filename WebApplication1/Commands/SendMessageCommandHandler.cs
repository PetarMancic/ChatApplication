using MediatR;
using WebApplication1.Models;
using WebApplication1.Notifications;
using WebApplication1.Services;

namespace WebApplication1.Commands;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ChatMessage>
{
    private readonly IChatMessageStore _store;
    private readonly IMediator _mediator;

    public SendMessageCommandHandler(IChatMessageStore store, IMediator mediator)
    {
        _store = store;
        _mediator = mediator;
    }

    public async Task<ChatMessage> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var message = new ChatMessage(request.ChannelId, request.User, request.Message, DateTime.UtcNow, request.SenderEmail);
        await _store.AddAsync(message);
        await _mediator.Publish(new MessageSentNotification(message), cancellationToken);
        return message;
    }
}
