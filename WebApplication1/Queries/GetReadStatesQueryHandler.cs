using MediatR;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Queries;

public class GetReadStatesQueryHandler : IRequestHandler<GetReadStatesQuery, IReadOnlyList<ReadState>>
{
    private readonly IReadStateStore _readStates;

    public GetReadStatesQueryHandler(IReadStateStore readStates)
    {
        _readStates = readStates;
    }

    public Task<IReadOnlyList<ReadState>> Handle(GetReadStatesQuery request, CancellationToken cancellationToken) =>
        _readStates.GetForChannelAsync(request.ChannelId);
}
