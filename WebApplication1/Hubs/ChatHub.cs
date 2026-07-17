using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebApplication1.Commands;
using WebApplication1.Queries;
using WebApplication1.Services;

namespace WebApplication1.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly IPresenceTracker _presence;

    public ChatHub(IMediator mediator, IPresenceTracker presence)
    {
        _mediator = mediator;
        _presence = presence;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (_presence.UserConnected(userId))
        {
            await Clients.Others.SendAsync("UserOnline", userId);
        }

        await Clients.Caller.SendAsync("OnlineUsers", _presence.GetOnlineUserIds());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (_presence.UserDisconnected(userId))
        {
            await Clients.Others.SendAsync("UserOffline", userId);
        }

        await base.OnDisconnectedAsync(exception);
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

    public async Task Typing(string channelId)
    {
        var userId = GetUserId();
        if (!await _mediator.Send(new IsChannelMemberQuery(channelId, userId)))
        {
            throw new HubException("You are not a member of this channel.");
        }

        var userName = Context.User?.Identity?.Name ?? "Unknown";
        await Clients.OthersInGroup(channelId).SendAsync("UserTyping", channelId, userId, userName);
    }

    private string GetUserId() =>
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new HubException("Unauthenticated.");
}
