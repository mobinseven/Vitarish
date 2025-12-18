namespace Vitarish.Entities.Server.SignalR;
/// <summary>
/// Tracks active SignalR hub connections for entity hubs.
/// Enables monitoring of connected clients and group memberships.
/// </summary>
public interface IEntityHubConnectionTracker
{
    void AddConnection(string connectionId, string? userId);
    void RemoveConnection(string connectionId);
    void AddToGroup(string connectionId, string groupName);
    void RemoveFromGroup(string connectionId, string groupName);
    IReadOnlyCollection<string> GetConnections();
    IReadOnlyCollection<string> GetConnectionsByUser(string userId);
    IReadOnlyCollection<string> GetConnectionsByGroup(string groupName);
    int GetConnectionCount();
}

public class EntityHubConnectionTracker : IEntityHubConnectionTracker
{
    private readonly ConcurrentDictionary<string, ConnectionInfo> _connections = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _userConnections = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _groupConnections = new();
    private readonly object _groupLock = new();

    private class ConnectionInfo
    {
        public string ConnectionId { get; }
        public string? UserId { get; }
        public ConcurrentDictionary<string, byte> Groups { get; } = new(StringComparer.OrdinalIgnoreCase);

        public ConnectionInfo(string connectionId, string? userId)
        {
            ConnectionId = connectionId;
            UserId = userId;
        }
    }

    public void AddConnection(string connectionId, string? userId)
    {
        _connections[connectionId] = new ConnectionInfo(connectionId, userId);

        if (!string.IsNullOrEmpty(userId))
        {
            _ = _userConnections.AddOrUpdate(
                userId,
                _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase) { [connectionId] = 0 },
                (n, existing) =>
                {
                    _ = existing.TryAdd(connectionId, 0);
                    return existing;
                });
        }
    }

    public void RemoveConnection(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out ConnectionInfo? info))
        {
            // Remove from user tracking
            if (!string.IsNullOrEmpty(info.UserId) &&
                _userConnections.TryGetValue(info.UserId, out ConcurrentDictionary<string, byte>? userConnections))
            {
                _ = userConnections.TryRemove(connectionId, out _);
                if (userConnections.IsEmpty)
                {
                    _ = _userConnections.TryRemove(info.UserId, out _);
                }
            }

            // Remove from all groups - lock covers entire operation to prevent race conditions
            // between creating snapshot and removing from _groupConnections
            lock (_groupLock)
            {
                List<string> groupsToRemove = info.Groups.Keys.ToList();
                foreach (string group in groupsToRemove)
                {
                    if (_groupConnections.TryGetValue(group, out ConcurrentDictionary<string, byte>? groupConnections))
                    {
                        _ = groupConnections.TryRemove(connectionId, out _);
                        if (groupConnections.IsEmpty)
                        {
                            _ = _groupConnections.TryRemove(group, out _);
                        }
                    }
                }
            }
        }
    }

    public void AddToGroup(string connectionId, string groupName)
    {
        if (_connections.TryGetValue(connectionId, out ConnectionInfo? info))
        {
            // Lock covers both info.Groups and _groupConnections modifications to prevent race conditions
            // with RemoveConnection which also needs consistent state between these two data structures
            lock (_groupLock)
            {
                _ = info.Groups.TryAdd(groupName, 0);

                _ = _groupConnections.AddOrUpdate(
                    groupName,
                    _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase) { [connectionId] = 0 },
                    (n, existing) =>
                    {
                        _ = existing.TryAdd(connectionId, 0);
                        return existing;
                    });
            }
        }
    }

    public void RemoveFromGroup(string connectionId, string groupName)
    {
        if (_connections.TryGetValue(connectionId, out ConnectionInfo? info))
        {
            // Lock covers both info.Groups and _groupConnections modifications to prevent race conditions
            // with AddToGroup which also needs consistent state between these two data structures
            lock (_groupLock)
            {
                _ = info.Groups.TryRemove(groupName, out _);

                if (_groupConnections.TryGetValue(groupName, out ConcurrentDictionary<string, byte>? groupConnections))
                {
                    _ = groupConnections.TryRemove(connectionId, out _);
                    if (groupConnections.IsEmpty)
                    {
                        _ = _groupConnections.TryRemove(groupName, out _);
                    }
                }
            }
        }
    }

    public IReadOnlyCollection<string> GetConnections() => _connections.Keys.ToList();

    public IReadOnlyCollection<string> GetConnectionsByUser(string userId) => _userConnections.TryGetValue(userId, out ConcurrentDictionary<string, byte>? connections)
                ? connections.Keys.ToList()
                : Array.Empty<string>();

    public IReadOnlyCollection<string> GetConnectionsByGroup(string groupName) => _groupConnections.TryGetValue(groupName, out ConcurrentDictionary<string, byte>? connections)
                ? connections.Keys.ToList()
                : Array.Empty<string>();

    public int GetConnectionCount() => _connections.Count;
}
