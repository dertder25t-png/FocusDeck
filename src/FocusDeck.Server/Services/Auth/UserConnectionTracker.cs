using System.Collections.Concurrent;

namespace FocusDeck.Server.Services.Auth;

public interface IUserConnectionTracker
{
    void Add(string userId, string connectionId);
    void Remove(string userId, string connectionId);
    IReadOnlyCollection<string> GetUserIds();
}

public class UserConnectionTracker : IUserConnectionTracker
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _map = new();

    public void Add(string userId, string connectionId)
    {
        var conns = _map.GetOrAdd(userId, _ => new ConcurrentDictionary<string, byte>());
        conns[connectionId] = 1;
    }

    public void Remove(string userId, string connectionId)
    {
        if (_map.TryGetValue(userId, out var conns))
        {
            conns.TryRemove(connectionId, out _);
            if (conns.IsEmpty)
                _map.TryRemove(userId, out _);
        }
    }

    public IReadOnlyCollection<string> GetUserIds()
    {
        return _map.Keys.ToList();
    }
}

