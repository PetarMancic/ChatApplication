using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebApplication1.Commands;
using WebApplication1.Queries;

namespace WebApplication1.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private const string GeneralRoom = "general";

    private readonly IMediator _mediator;

    public ChatHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GeneralRoom);
        var history = await _mediator.Send(new GetMessageHistoryQuery());
        await Clients.Caller.SendAsync("ReceiveHistory", history);
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(string message)
    {
        var userName = Context.User?.Identity?.Name ?? "Unknown";
        await _mediator.Send(new SendMessageCommand(userName, message));
    }
}
