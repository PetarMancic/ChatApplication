using MediatR;
using WebApplication1.Services;

namespace WebApplication1.Commands;

public class MarkReadCommandHandler : IRequestHandler<MarkReadCommand, bool>
{
    private readonly IReadStateStore _readStates;

    public MarkReadCommandHandler(IReadStateStore readStates)
    {
        _readStates = readStates;
    }

    public Task<bool> Handle(MarkReadCommand request, CancellationToken cancellationToken) =>
        _readStates.UpsertIfNewerAsync(request.UserId, request.ChannelId, request.MessageId);
}
