using System.Diagnostics.CodeAnalysis;
using System.IO;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace FocusDeck.Server.Services.Jarvis;

public interface IJarvisWorkflowRegistry
{
    /// <summary>
    /// Returns the list of Jarvis workflows available for the current deployment.
    /// </summary>
    Task<IReadOnlyList<JarvisWorkflowSummaryDto>> ListWorkflowsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues a workflow run and returns a lightweight run descriptor.
    /// </summary>
    Task<JarvisRunResponseDto> EnqueueWorkflowAsync(JarvisRunRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current status for a workflow run, or null if not found.
    /// </summary>
    Task<JarvisRunStatusDto?> GetRunStatusAsync(Guid runId, CancellationToken cancellationToken = default);
}

public sealed class JarvisRunLimitExceededException : Exception
{
    public JarvisRunLimitExceededException(string message) : base(message)
    {
    }
}

/// <summary>
/// Minimal Jarvis workflow registry stub. In Phase 3.1 this only exposes
/// a thin, well-documented surface; later phases will scan the filesystem
/// and wire Hangfire jobs / persistence.
/// </summary>
public sealed class JarvisWorkflowRegistry : IJarvisWorkflowRegistry
{
    private const int MaxActiveRunsPerUser = 3;
    private readonly ILogger<JarvisWorkflowRegistry> _logger;
    private readonly AutomationDbContext _db;
    private readonly IBackgroundJobClient _jobs;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHostEnvironment _hostEnvironment;

    public JarvisWorkflowRegistry(
        AutomationDbContext db,
        IBackgroundJobClient jobs,
        IHttpContextAccessor httpContextAccessor,
        IHostEnvironment hostEnvironment,
        ILogger<JarvisWorkflowRegistry> logger)
    {
        _db = db;
        _jobs = jobs;
        _httpContextAccessor = httpContextAccessor;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public Task<IReadOnlyList<JarvisWorkflowSummaryDto>> ListWorkflowsAsync(CancellationToken cancellationToken = default)
    {
        var contentRoot = _hostEnvironment.ContentRootPath ?? Directory.GetCurrentDirectory();
        var workflowsRoot = Path.Combine(contentRoot, "bmad", "jarvis", "workflows");

        if (!Directory.Exists(workflowsRoot))
        {
            _logger.LogInformation("Jarvis workflows directory not found at {WorkflowsRoot}. Returning empty list.", workflowsRoot);
            return Task.FromResult<IReadOnlyList<JarvisWorkflowSummaryDto>>(Array.Empty<JarvisWorkflowSummaryDto>());
        }

        var results = new List<JarvisWorkflowSummaryDto>();

        foreach (var file in Directory.EnumerateFiles(workflowsRoot, "workflow.yaml", SearchOption.AllDirectories))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                if (TryParseWorkflowFile(file, out var summary))
                {
                    results.Add(summary);
                }
                else
                {
                    _logger.LogWarning("Jarvis workflow file {WorkflowFile} is missing required metadata and will be skipped.", file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Jarvis workflow file {WorkflowFile}; skipping.", file);
            }
        }

        var ordered = results
            .OrderBy(w => w.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _logger.LogInformation("Discovered {WorkflowCount} Jarvis workflows under {WorkflowsRoot}.", ordered.Length, workflowsRoot);
        return Task.FromResult<IReadOnlyList<JarvisWorkflowSummaryDto>>(ordered);
    }

    public async Task<JarvisRunResponseDto> EnqueueWorkflowAsync(JarvisRunRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.WorkflowId))
        {
            throw new ArgumentException("WorkflowId is required.", nameof(request));
        }

        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? "unknown";

        var activeCount = await _db.JarvisWorkflowRuns
            .Where(r => r.RequestedByUserId == userId && (r.Status == "Queued" || r.Status == "Running"))
            .CountAsync(cancellationToken);

        if (activeCount >= MaxActiveRunsPerUser)
        {
            throw new JarvisRunLimitExceededException(
                $"Too many active Jarvis runs for this user. Limit is {MaxActiveRunsPerUser} concurrent/queued runs.");
        }

        var run = new JarvisWorkflowRun
        {
            Id = Guid.NewGuid(),
            WorkflowId = request.WorkflowId.Trim(),
            Status = "Queued",
            RequestedAtUtc = DateTime.UtcNow,
            RequestedByUserId = userId,
            LogSummary = "Jarvis workflow run queued.",
            // TenantId will be stamped by AutomationDbContext using ICurrentTenant
        };

        await _db.JarvisWorkflowRuns.AddAsync(run, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        var tenantId = run.TenantId == Guid.Empty ? (Guid?)null : run.TenantId;
        JarvisTelemetry.RecordRunStarted(tenantId, run.WorkflowId);

        var jobId = _jobs.Enqueue<JarvisWorkflowJob>(job => job.ExecuteAsync(run.Id, default));
        run.JobId = jobId;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Jarvis workflow run enqueued. WorkflowId={WorkflowId}, RunId={RunId}, JobId={JobId}",
            request.WorkflowId, run.Id, jobId);

        return new JarvisRunResponseDto(run.Id);
    }

    public async Task<JarvisRunStatusDto?> GetRunStatusAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _db.JarvisWorkflowRuns.FindAsync(new object[] { runId }, cancellationToken);
        if (run == null)
        {
            return null;
        }

        return new JarvisRunStatusDto(
            run.Id,
            run.Status,
            run.LogSummary);
    }

    private static bool TryParseWorkflowFile(string path, [NotNullWhen(true)] out JarvisWorkflowSummaryDto? summary)
    {
        summary = null;

        string? name = null;
        string? description = null;

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("name:", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(name))
            {
                var value = line.Substring("name:".Length).Trim();
                name = TrimQuotes(value);
            }
            else if (line.StartsWith("description:", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(description))
            {
                var value = line.Substring("description:".Length).Trim();
                description = TrimQuotes(value);
            }

            if (!string.IsNullOrWhiteSpace(name) && description is not null)
            {
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var id = name;
        var displayName = name;

        summary = new JarvisWorkflowSummaryDto(id, displayName, description);
        return true;
    }

    private static string? TrimQuotes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var trimmed = value.Trim();
        if (trimmed.Length >= 2 &&
            ((trimmed.StartsWith("\"", StringComparison.Ordinal) && trimmed.EndsWith("\"", StringComparison.Ordinal)) ||
             (trimmed.StartsWith("'", StringComparison.Ordinal) && trimmed.EndsWith("'", StringComparison.Ordinal))))
        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2).Trim();
        }

        return trimmed;
    }
}
