using WebApplication1.Models;

namespace WebApplication1.Services;

public class InMemoryChatMessageStore : IChatMessageStore
{
    private readonly List<ChatMessage> _messages = [];
    private readonly object _lock = new();

    public void Add(ChatMessage message)
    {
        lock (_lock)
        {
            _messages.Add(message);
        }
    }

    public IReadOnlyList<ChatMessage> GetAll()
    {
        lock (_lock)
        {
            return _messages.ToList();
        }
    }
}
