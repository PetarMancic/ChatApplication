using System.Collections.Concurrent;

namespace WebApplication1.Services;

public class InMemoryPresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<string, int> _connectionCounts = new();

    public bool UserConnected(string userId)
    {
        var count = _connectionCounts.AddOrUpdate(userId, 1, (_, current) => current + 1);
        return count == 1;
    }

    public bool UserDisconnected(string userId)
    {
        while (true)
        {
            if (!_connectionCounts.TryGetValue(userId, out var current))
            {
                return false;
            }

            if (current <= 1)
            {
                // Remove only if the value is still what we read (another connection may have raced us)
                if (_connectionCounts.TryRemove(new KeyValuePair<string, int>(userId, current)))
                {
                    return true;
                }
            }
            else if (_connectionCounts.TryUpdate(userId, current - 1, current))
            {
                return false;
            }
        }
    }

    public IReadOnlyList<string> GetOnlineUserIds() => _connectionCounts.Keys.ToList();
}
