using System.Security.Claims;
using Asp.Versioning;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Server.Services.Jarvis;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    private readonly ISuggestionService _suggestionService;
    private readonly IFeedbackService _feedbackService;
    private readonly FocusDeck.Contracts.Services.Context.IContextRetrievalService _retrievalService;
    private readonly FocusDeck.Persistence.AutomationDbContext _dbContext;
    private readonly FocusDeck.SharedKernel.Tenancy.ICurrentTenant _currentTenant;
    private readonly IAutomationGeneratorService _automationGenerator;

    public JarvisController(
        IJarvisWorkflowRegistry registry,
        ILogger<JarvisController> logger,
        IConfiguration configuration,
        ISuggestionService suggestionService,
        IFeedbackService feedbackService,
        FocusDeck.Contracts.Services.Context.IContextRetrievalService retrievalService,
        FocusDeck.Persistence.AutomationDbContext dbContext,
        FocusDeck.SharedKernel.Tenancy.ICurrentTenant currentTenant,
        IAutomationGeneratorService automationGenerator)
    {
        _registry = registry;
        _logger = logger;
        _isEnabled = configuration.GetValue<bool>("Features:Jarvis", false);
        _suggestionService = suggestionService;
        _feedbackService = feedbackService;
        _retrievalService = retrievalService;
        _dbContext = dbContext;
        _currentTenant = currentTenant;
        _automationGenerator = automationGenerator;
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

    /// <summary>
    /// Generates a suggestion based on the user's current context.
    /// </summary>
    [HttpPost("suggest")]
    public async Task<ActionResult<SuggestionResponseDto>> GetSuggestion([FromBody] SuggestionRequestDto request)
    {
        if (!_isEnabled)
        {
            return NotFound(new { error = "Jarvis feature is disabled." });
        }

        var suggestion = await _suggestionService.GenerateSuggestionAsync(request);
        if (suggestion == null)
        {
            return NoContent();
        }

        return Ok(suggestion);
    }

    /// <summary>
    /// Submits feedback for a given suggestion.
    /// </summary>
    [HttpPost("feedback")]
    public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequestDto request)
    {
        if (!_isEnabled)
        {
            return NotFound(new { error = "Jarvis feature is disabled." });
        }

        await _feedbackService.ProcessFeedbackAsync(request);
        return Accepted();
    }

    /// <summary>
    /// Retrieves similar past moments based on the user's most recent context snapshot.
    /// </summary>
    [HttpGet("suggest-moments")]
    public async Task<ActionResult<IEnumerable<FocusDeck.Domain.Entities.Context.ContextSnapshot>>> GetSimilarMoments()
    {
        if (!_isEnabled)
        {
            return NotFound(new { error = "Jarvis feature is disabled." });
        }

        // Fetch the latest snapshot for the current user/tenant
        // We need to perform this query manually as we don't have the full snapshot object in the request
        var latestSnapshot = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
            _dbContext.ContextSnapshots
                .Include(s => s.Slices)
                .Include(s => s.Metadata)
                .OrderByDescending(s => s.Timestamp));

        if (latestSnapshot == null)
        {
            return NotFound(new { error = "No context history found to generate suggestions from." });
        }

        var similarMoments = await _retrievalService.GetSimilarMomentsAsync(latestSnapshot);
        return Ok(similarMoments);
    }

    /// <summary>
    /// Generates an automation proposal based on user intent (Architect mode).
    /// </summary>
    [HttpPost("architect/generate")]
    public async Task<ActionResult> GenerateFromIntent([FromBody] GenerateIntentRequest request)
    {
        if (!_isEnabled) return NotFound(new { error = "Jarvis is disabled" });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        try
        {
            var proposal = await _automationGenerator.GenerateProposalFromIntentAsync(request.Intent, userId);
            return Ok(new { proposalId = proposal.Id, yaml = proposal.YamlDefinition });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to generate automation from intent");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public record GenerateIntentRequest(string Intent);
