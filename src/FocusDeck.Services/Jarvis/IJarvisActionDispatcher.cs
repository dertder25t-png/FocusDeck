using System.Threading;
using System.Threading.Tasks;

namespace FocusDeck.Services.Jarvis
{
    public interface IJarvisActionDispatcher
    {
        Task<string> DispatchAsync(string userId, string actionName, string? inputJson, CancellationToken ct);
    }
}
