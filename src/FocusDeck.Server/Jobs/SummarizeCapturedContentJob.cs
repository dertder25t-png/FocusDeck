using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Services.Context;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Jobs
{
    public class SummarizeCapturedContentJob
    {
        private readonly KnowledgeVaultService _service;
        private readonly ILogger<SummarizeCapturedContentJob> _logger;

        public SummarizeCapturedContentJob(
            KnowledgeVaultService service,
            ILogger<SummarizeCapturedContentJob> logger)
        {
            _service = service;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await _service.SummarizePendingItemsAsync(cancellationToken);
        }
    }
}
