using System.Threading;
using System.Threading.Tasks;

namespace FocusDeck.Services.Jarvis
{
    public class NoOpActionHandler : IJarvisActionHandler
    {
        public string ActionName => "noop";

        public Task<string> ExecuteAsync(string userId, string? inputJson, CancellationToken ct)
        {
            // TODO: just return some placeholder result
            return Task.FromResult("{\"result\":\"noop-ok\"}");
        }
    }
}
