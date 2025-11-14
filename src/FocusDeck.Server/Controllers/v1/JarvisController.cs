using Asp.Versioning;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Server.Services.Jarvis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FocusDeck.Server.Controllers.v1;

/// <summary>
/// Jarvis API surface (Phase 3.1 stub).
/// 
/// Exposes workflow discovery and a thin run-enqueue endpoint backed by
/// <see cref="IJarvisWorkflowRegistry"/>. Later phases will attach Hangfire
/// jobs, persistence, and SignalR dispatch; for now the endpoints are
/// intentionally lightweight and feature-gated at the caller level.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/jarvis")]
[Authorize]
public sealed class JarvisController : ControllerBase
{
    private readonly IJarvisWorkflowRegistry _registry;
    private readonly ILogger<JarvisController> _logger;
    private readonly bool _isEnabled;

    public JarvisController(IJarvisWorkflowRegistry registry, ILogger<JarvisController> logger, IConfiguration configuration)
    {
        _registry = registry;
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("Features:Jarvis", false);
    }

    /// <summary>
    /// Lists Jarvis workflows known to the server.
    /// 
    /// Phase 3.1: returns an empty list until the registry is wired to
    /// scan the bmad/jarvis/workflows tree.
    /// </summary>
    [HttpGet("workflows")]
    public async Task<ActionResult<IReadOnlyList<JarvisWorkflowSummaryDto>>> GetWorkflows(CancellationToken cancellationToken)
    {
        if (!_isEnabled)
        {
            return NotFound(new { error = "Jarvis feature is disabled." });
        }

        var workflows = await _registry.ListWorkflowsAsync(cancellationToken);
        return Ok(workflows);
    }

    /// <summary>
    /// Enqueues a Jarvis workflow run.
    /// 
    /// Phase 3.1: creates a synthetic run identifier via the registry. Later
    /// phases will persist runs and attach a Hangfire job + SignalR dispatch.
    /// </summary>
    [HttpPost("run-workflow")]
    public async Task<ActionResult<JarvisRunResponseDto>> RunWorkflow([FromBody] JarvisRunRequestDto request, CancellationToken cancellationToken)
    {
        if (!_isEnabled)
        {
            return NotFound(new { error = "Jarvis is not enabled for this environment. Ask an administrator to enable Features:Jarvis for your tenant." });
        }

        if (request is null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        if (string.IsNullOrWhiteSpace(request.WorkflowId))
        {
            return BadRequest(new { error = "WorkflowId is required." });
        }

        try
        {
            var response = await _registry.EnqueueWorkflowAsync(request, cancellationToken);
            _logger.LogInformation("Jarvis run requested. WorkflowId={WorkflowId}, RunId={RunId}", request.WorkflowId, response.RunId);
            return Ok(response);
        }
        catch (JarvisRunLimitExceededException ex)
        {
            _logger.LogWarning(ex, "Jarvis run limit exceeded for request WorkflowId={WorkflowId}", request.WorkflowId);
            return StatusCode(StatusCodes.Status429TooManyRequests, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Returns the current status for a workflow run.
    /// 
    /// Phase 3.1: returns a placeholder "Pending" status until run
    /// persistence is implemented in later phases.
    /// </summary>
    [HttpGet("runs/{id:guid}")]
    public async Task<ActionResult<JarvisRunStatusDto>> GetRunStatus(Guid id, CancellationToken cancellationToken)
    {
        if (!_isEnabled)
        {
            return NotFound(new { error = "Jarvis feature is disabled." });
        }

        var status = await _registry.GetRunStatusAsync(id, cancellationToken);
        if (status is null)
        {
            return NotFound(new { error = "Run not found." });
        }

        return Ok(status);
    }
}
