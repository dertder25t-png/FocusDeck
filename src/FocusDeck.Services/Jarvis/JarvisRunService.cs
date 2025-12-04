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
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("UserId is required", nameof(userId));
            }

            if (string.IsNullOrWhiteSpace(entryPoint))
            {
                throw new ArgumentException("EntryPoint is required", nameof(entryPoint));
            }

            var run = new JarvisRun
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EntryPoint = entryPoint,
                InputPayloadJson = inputPayloadJson,
                Status = JarvisRunStatus.Running,
                StartedAt = DateTimeOffset.UtcNow
            };

            await _repo.AddAsync(run, ct);
            _logger.LogInformation("Started Jarvis run {RunId} for user {UserId}, entryPoint={EntryPoint}", 
                run.Id, userId, entryPoint);

            return run;
        }

        public async Task AppendStepAsync(Guid runId, JarvisRunStep step, CancellationToken ct)
        {
            if (runId == Guid.Empty)
            {
                throw new ArgumentException("RunId is required", nameof(runId));
            }

            if (step == null)
            {
                throw new ArgumentNullException(nameof(step));
            }

            var run = await _repo.GetByIdAsync(runId, ct);
            if (run == null)
            {
                throw new InvalidOperationException($"Jarvis run {runId} not found");
            }

            // Set step properties
            step.Id = Guid.NewGuid();
            step.RunId = runId;
            step.Order = run.Steps.Count + 1;
            step.CreatedAt = DateTimeOffset.UtcNow;

            run.Steps.Add(step);
            await _repo.UpdateAsync(run, ct);

            _logger.LogDebug("Appended step {StepOrder} to Jarvis run {RunId}", step.Order, runId);
        }

        public async Task CompleteRunAsync(Guid runId, JarvisRunStatus finalStatus, CancellationToken ct)
        {
            if (runId == Guid.Empty)
            {
                throw new ArgumentException("RunId is required", nameof(runId));
            }

            var run = await _repo.GetByIdAsync(runId, ct);
            if (run == null)
            {
                throw new InvalidOperationException($"Jarvis run {runId} not found");
            }

            run.Status = finalStatus;
            run.CompletedAt = DateTimeOffset.UtcNow;

            await _repo.UpdateAsync(run, ct);

            _logger.LogInformation("Completed Jarvis run {RunId} with status {Status}", runId, finalStatus);
        }
    }
}
