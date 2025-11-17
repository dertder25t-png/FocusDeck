using System;
using System.Threading.Tasks;
using FocusDeck.Server.Services.Jarvis;
using FocusDeck.Services.Jarvis;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Jobs.Jarvis
{
    public class JarvisRunJob : IJarvisRunJob
    {
        private readonly IJarvisRunService _runService;
        private readonly IJarvisActionDispatcher _dispatcher;
        private readonly ILayeredContextService _layeredContext;
        private readonly IExampleGenerator _exampleGenerator;
        private readonly ILogger<JarvisRunJob> _logger;

        public JarvisRunJob(
            IJarvisRunService runService,
            IJarvisActionDispatcher dispatcher,
            ILayeredContextService layeredContext,
            IExampleGenerator exampleGenerator,
            ILogger<JarvisRunJob> logger)
        {
            _runService = runService;
            _dispatcher = dispatcher;
            _layeredContext = layeredContext;
            _exampleGenerator = exampleGenerator;
            _logger = logger;
        }

        public Task ExecuteAsync(Guid runId)
        {
            // TODO:
            // 1. Load run from repository.
            // 2. Build LayeredContextDto via ILayeredContextService.
            // 3. Get few-shot examples via IExampleGenerator.
            // 4. (Future) Call LLM to decide actionName + inputJson.
            // 5. Dispatch action via IJarvisActionDispatcher.
            // 6. Append JarvisRunStep(s) with request/response JSON.
            // 7. Complete run.

            throw new NotImplementedException();
        }
    }
}
