using MediatR;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Queries;

public class GetMessageHistoryQueryHandler : IRequestHandler<GetMessageHistoryQuery, IReadOnlyList<ChatMessage>>
{
    private readonly IChatMessageStore _store;

    public GetMessageHistoryQueryHandler(IChatMessageStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<ChatMessage>> Handle(GetMessageHistoryQuery request, CancellationToken cancellationToken) =>
        _store.GetAllAsync();
}
