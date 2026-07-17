namespace WebApplication1.Services;

public interface IPresenceTracker
{
    /// <summary>Returns true only when this connection takes the user from offline to online (0 → 1).</summary>
    bool UserConnected(string userId);

    /// <summary>Returns true only when this disconnect takes the user from online to offline (1 → 0).</summary>
    bool UserDisconnected(string userId);

    IReadOnlyList<string> GetOnlineUserIds();
}
