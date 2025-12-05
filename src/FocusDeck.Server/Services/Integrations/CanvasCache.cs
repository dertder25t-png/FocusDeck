using FocusDeck.Services.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace FocusDeck.Server.Services.Integrations;

public interface ICanvasCache
{
    void SetAssignments(List<CanvasAssignment> items, TimeSpan ttl);
    List<CanvasAssignment> GetAssignments();
}

public class CanvasCache : ICanvasCache
{
    private readonly IMemoryCache _cache;
    private const string Key = "canvas_assignments";

    public CanvasCache(IMemoryCache memoryCache)
    {
        _cache = memoryCache;
    }

    public void SetAssignments(List<CanvasAssignment> items, TimeSpan ttl)
    {
        _cache.Set(Key, items, ttl);
    }

    public List<CanvasAssignment> GetAssignments()
    {
        return _cache.TryGetValue(Key, out List<CanvasAssignment>? items) ? items! : new List<CanvasAssignment>();
    }
}
