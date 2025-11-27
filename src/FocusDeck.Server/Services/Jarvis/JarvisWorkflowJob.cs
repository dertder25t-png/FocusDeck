using FocusDeck.Persistence;
using Hangfire;
using FocusDeck.Server.Hubs;
using FocusDeck.Shared.SignalR.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Jarvis;

/// <summary>
/// Hangfire job responsible for executing a Jarvis workflow run.
/// Phase 3.2: minimal implementation that only updates status and logs
/// a synthetic "workflow executed" message; no external script calls yet.
/// </summary>
public sealed class JarvisWorkflowJob
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<JarvisWorkflowJob> _logger;
    private readonly IHubContext<NotificationsHub, INotificationClient> _hub;

    public JarvisWorkflowJob(
        AutomationDbContext db,
        IHubContext<NotificationsHub, INotificationClient> hub,
        ILogger<JarvisWorkflowJob> logger)
    {
        _db = db;
        _hub = hub;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task ExecuteAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await _db.JarvisWorkflowRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run == null)
        {
            _logger.LogWarning("JarvisWorkflowJob invoked with unknown runId={RunId}", runId);
            return;
        }

        if (run.Status is "Succeeded" or "Failed")
        {
            _logger.LogInformation("JarvisWorkflowJob runId={RunId} already completed with status={Status}", runId, run.Status);
            return;
        }

            try
            {
            run.Status = "Running";
            run.StartedAtUtc ??= DateTime.UtcNow;
            run.LogSummary = "Jarvis workflow execution started.";
            await _db.SaveChangesAsync(cancellationToken);
            LogState(run, "Running");
            await NotifyAsync(run);

            // Phase 3.2 stub: replace this block with actual bmad.ps1 or workflow engine call in a later phase.
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

            run.Status = "Succeeded";
            run.CompletedAtUtc = DateTime.UtcNow;
            run.LogSummary = "Jarvis workflow executed successfully (stub).";
            await _db.SaveChangesAsync(cancellationToken);

            LogState(run, "Succeeded");
            _logger.LogInformation("JarvisWorkflowJob completed successfully. RunId={RunId}, WorkflowId={WorkflowId}, TenantId={TenantId}, RequestedByUserId={UserId}", run.Id, run.WorkflowId, run.TenantId, run.RequestedByUserId);
        }
        catch (OperationCanceledException)
        {
            run.Status = "Failed";
            run.CompletedAtUtc = DateTime.UtcNow;
            run.LogSummary = "Jarvis workflow execution cancelled.";
            await _db.SaveChangesAsync(CancellationToken.None);
            LogState(run, "Failed");
            _logger.LogWarning("JarvisWorkflowJob cancelled. RunId={RunId}, WorkflowId={WorkflowId}, TenantId={TenantId}, RequestedByUserId={UserId}", run.Id, run.WorkflowId, run.TenantId, run.RequestedByUserId);
            await NotifyAsync(run);
        }
        catch (Exception ex)
        {
            run.Status = "Failed";
            run.CompletedAtUtc = DateTime.UtcNow;
            run.LogSummary = $"Jarvis workflow execution failed: {ex.Message}";
            await _db.SaveChangesAsync(CancellationToken.None);
            LogState(run, "Failed", ex.Message);
            _logger.LogError(ex, "JarvisWorkflowJob failed. RunId={RunId}, WorkflowId={WorkflowId}, TenantId={TenantId}, RequestedByUserId={UserId}", run.Id, run.WorkflowId, run.TenantId, run.RequestedByUserId);
            await NotifyAsync(run);
        }
    }

    private void LogState(FocusDeck.Domain.Entities.JarvisWorkflowRun run, string targetStatus, string? errorReason = null)
    {
        var tenantId = run.TenantId == Guid.Empty ? (Guid?)null : run.TenantId;
        var durationSeconds = 0.0;

        if (run.CompletedAtUtc.HasValue && run.RequestedAtUtc != default)
        {
            durationSeconds = (run.CompletedAtUtc.Value - run.RequestedAtUtc).TotalSeconds;
            if (durationSeconds < 0)
            {
                durationSeconds = 0;
            }
        }

        switch (targetStatus)
        {
            case "Succeeded":
                JarvisTelemetry.RecordRunSucceeded(tenantId, run.WorkflowId, durationSeconds);
                break;
            case "Failed":
                JarvisTelemetry.RecordRunFailed(tenantId, run.WorkflowId, durationSeconds, errorReason ?? "unknown");
                break;
            case "Running":
            case "Queued":
            default:
                // Only track started metric once when run is initially enqueued (registry),
                // so we don't emit anything here for Running.
                break;
        }
    }

    private async Task NotifyAsync(FocusDeck.Domain.Entities.JarvisWorkflowRun run)
    {
        try
        {
            if (run.TenantId == Guid.Empty || string.IsNullOrWhiteSpace(run.RequestedByUserId))
            {
                return;
            }

            var payload = new JarvisRunUpdate(
                run.Id,
                run.WorkflowId,
                run.Status,
                run.LogSummary,
                DateTime.UtcNow);

            await _hub.Clients.Group($"user:{run.RequestedByUserId}").JarvisRunUpdated(payload);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send JarvisRunUpdated notification for RunId={RunId}", run.Id);
        }
    }
}
