using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebApplication1.Commands;
using WebApplication1.Queries;

namespace WebApplication1.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;

    public ChatHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task JoinChannel(string channelId)
    {
        var userId = GetUserId();
        if (!await _mediator.Send(new IsChannelMemberQuery(channelId, userId)))
        {
            throw new HubException("You are not a member of this channel.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, channelId);
        var history = await _mediator.Send(new GetMessageHistoryQuery(channelId));
        await Clients.Caller.SendAsync("ReceiveHistory", channelId, history);
    }

    public Task LeaveChannel(string channelId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, channelId);

    public async Task SendMessage(string channelId, string message)
    {
        var userId = GetUserId();
        if (!await _mediator.Send(new IsChannelMemberQuery(channelId, userId)))
        {
            throw new HubException("You are not a member of this channel.");
        }

        var userName = Context.User?.Identity?.Name ?? "Unknown";
        var email = Context.User?.FindFirstValue(ClaimTypes.Email) ?? "";
        await _mediator.Send(new SendMessageCommand(channelId, userName, email, message));
    }

    private string GetUserId() =>
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException("Unauthenticated.");
}
