using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Services.Jarvis
{
    public class JarvisActionDispatcher : IJarvisActionDispatcher
    {
        private readonly IEnumerable<IJarvisActionHandler> _handlers;
        private readonly ILogger<JarvisActionDispatcher> _logger;

        public JarvisActionDispatcher(IEnumerable<IJarvisActionHandler> handlers, ILogger<JarvisActionDispatcher> logger)
        {
            _handlers = handlers;
            _logger = logger;
        }

        public Task<string> DispatchAsync(string userId, string actionName, string? inputJson, CancellationToken ct)
        {
            // TODO: find handler.ActionName == actionName (case-insensitive)
            // if not found, log + throw
            // else call ExecuteAsync(userId, inputJson, ct)
            throw new System.NotImplementedException();
        }
    }
}
