using WebApplication1.Models;

namespace WebApplication1.Services;

public interface IChatMessageStore
{
    void Add(ChatMessage message);
    IReadOnlyList<ChatMessage> GetAll();
}
