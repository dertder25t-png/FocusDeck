using System.Threading;
using System.Threading.Tasks;

namespace FocusDeck.Services.Jarvis
{
    public interface IJarvisActionHandler
    {
        string ActionName { get; } // e.g. "start_study_session", "open_layout"
        Task<string> ExecuteAsync(string userId, string? inputJson, CancellationToken ct);
    }
}
