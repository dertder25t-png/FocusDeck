using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Contracts.Repositories;
using FocusDeck.Domain.Entities.Jarvis;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Services.Jarvis
{
    public class JarvisRunService : IJarvisRunService
    {
        private readonly IJarvisRunRepository _repo;
        private readonly ILogger<JarvisRunService> _logger;

        public JarvisRunService(IJarvisRunRepository repo, ILogger<JarvisRunService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<JarvisRun> StartRunAsync(string userId, string entryPoint, string? inputPayloadJson, CancellationToken ct)
        {
            // TODO: create new JarvisRun with Status = Pending or Running
            //  - set StartedAt
            //  - store InputPayloadJson
            //  - save via repo
            throw new NotImplementedException();
        }

        public Task AppendStepAsync(Guid runId, JarvisRunStep step, CancellationToken ct)
        {
            // TODO: load run, set step.Order = run.Steps.Count + 1,
            //  add step, save
            throw new NotImplementedException();
        }

        public Task CompleteRunAsync(Guid runId, JarvisRunStatus finalStatus, CancellationToken ct)
        {
            // TODO: update Status + CompletedAt, save
            throw new NotImplementedException();
        }
    }
}
