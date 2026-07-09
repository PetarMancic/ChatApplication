using Microsoft.AspNetCore.SignalR;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Hubs;

public class ChatHub : Hub
{
    private const string GeneralRoom = "general";

    private readonly IChatMessageStore _store;

    public ChatHub(IChatMessageStore store)
    {
        _store = store;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GeneralRoom);
        await Clients.Caller.SendAsync("ReceiveHistory", _store.GetAll());
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(string user, string message)
    {
        var chatMessage = new ChatMessage(user, message, DateTime.UtcNow);
        _store.Add(chatMessage);
        await Clients.Group(GeneralRoom).SendAsync(
            "ReceiveMessage", chatMessage.User, chatMessage.Message, chatMessage.Timestamp);
    }
}
