using MediatR;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Queries;

public class GetMessagesAfterQueryHandler : IRequestHandler<GetMessagesAfterQuery, IReadOnlyList<ChatMessage>>
{
    private readonly IChatMessageStore _store;

    public GetMessagesAfterQueryHandler(IChatMessageStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<ChatMessage>> Handle(GetMessagesAfterQuery request, CancellationToken cancellationToken) =>
        _store.GetAfterAsync(request.ChannelId, request.AfterMessageId);
}
