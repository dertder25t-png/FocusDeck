using Asp.Versioning;
using FocusDeck.Persistence;
using FocusDeck.Domain.Entities;
using FocusDeck.Domain.Entities.Remote;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Server.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1;

/// <summary>
/// Controller for managing remote actions
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/remote")]
[ApiController]
public class RemoteController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<RemoteController> _logger;
    private readonly IHubContext<NotificationsHub, INotificationClient> _hubContext;

    public RemoteController(
        AutomationDbContext db, 
        ILogger<RemoteController> logger,
        IHubContext<NotificationsHub, INotificationClient> hubContext)
    {
        _db = db;
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Get current user ID from claims
    /// </summary>
    private string GetUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }
        
        return userId;
    }

    /// <summary>
    /// Create a new remote action
    /// </summary>
    /// <param name="request">Action details</param>
    /// <returns>Created action</returns>
    [HttpPost("actions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RemoteActionDto>> CreateAction([FromBody] CreateRemoteActionDto request)
    {
        var userId = GetUserId();

        // Validate action kind
        if (!Enum.TryParse<RemoteActionKind>(request.Kind, true, out var actionKind))
        {
            return BadRequest(new { error = $"Invalid action kind: {request.Kind}" });
        }

        // Create remote action
        var action = new RemoteAction
        {
            UserId = userId,
            Kind = actionKind,
            CreatedAt = DateTime.UtcNow
        };

        action.SetPayload(request.Payload);

        _db.RemoteActions.Add(action);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Remote action created: {ActionId} ({Kind}) for user {UserId}", 
            action.Id, actionKind, userId);

        // Send SignalR notification to desktop clients
        await _hubContext.Clients.Group($"user:{userId}")
            .ReceiveNotification(
                "Remote Action",
                $"New action: {actionKind}",
                "info");

        var dto = MapToDto(action);
        return CreatedAtAction(nameof(GetAction), new { id = action.Id }, dto);
    }

    /// <summary>
    /// Get a remote action by ID
    /// </summary>
    /// <param name="id">Action ID</param>
    /// <returns>Action details</returns>
    [HttpGet("actions/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemoteActionDto>> GetAction(Guid id)
    {
        var userId = GetUserId();

        var action = await _db.RemoteActions
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (action == null)
        {
            return NotFound(new { error = "Action not found" });
        }

        return Ok(MapToDto(action));
    }

    /// <summary>
    /// Get all remote actions for the current user
    /// </summary>
    /// <param name="pending">Filter by pending status</param>
    /// <param name="limit">Maximum number of actions to return</param>
    /// <returns>List of actions</returns>
    [HttpGet("actions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RemoteActionDto>>> GetActions(
        [FromQuery] bool? pending = null,
        [FromQuery] int limit = 100)
    {
        var userId = GetUserId();

        var query = _db.RemoteActions
            .Where(a => a.UserId == userId);

        if (pending.HasValue)
        {
            if (pending.Value)
            {
                query = query.Where(a => a.CompletedAt == null);
            }
            else
            {
                query = query.Where(a => a.CompletedAt != null);
            }
        }

        var actions = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(Math.Min(limit, 1000))
            .ToListAsync();

        var dtos = actions.Select(MapToDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Complete a remote action
    /// </summary>
    /// <param name="id">Action ID</param>
    /// <param name="request">Completion details</param>
    /// <returns>Success status</returns>
    [HttpPost("actions/{id}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CompleteAction(Guid id, [FromBody] CompleteRemoteActionDto request)
    {
        var userId = GetUserId();

        var action = await _db.RemoteActions
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

        if (action == null)
        {
            return NotFound(new { error = "Action not found" });
        }

        action.CompletedAt = DateTime.UtcNow;
        action.Success = request.Success;
        action.ErrorMessage = request.ErrorMessage;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Remote action completed: {ActionId} (Success={Success})", 
            action.Id, request.Success);

        return Ok(new { success = true });
    }

    /// <summary>
    /// Get remote telemetry summary
    /// </summary>
    /// <returns>Telemetry data</returns>
    [HttpGet("telemetry/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<RemoteTelemetrySummaryDto>> GetTelemetrySummary()
    {
        var userId = GetUserId();

        // Check for active study sessions
        var activeSession = await _db.StudySessions
            .Where(s => s.Status == SessionStatus.Active)
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync();

        // Calculate progress (simplified - based on session duration)
        int progressPercent = 0;
        if (activeSession != null)
        {
            var elapsed = (DateTime.UtcNow - activeSession.StartTime).TotalMinutes;
            progressPercent = Math.Min(100, (int)(elapsed / activeSession.DurationMinutes * 100));
        }

        // Get most recently modified note
        var recentNote = await _db.Notes
            .OrderByDescending(n => n.LastModified ?? n.CreatedDate)
            .FirstOrDefaultAsync();

        return Ok(new RemoteTelemetrySummaryDto
        {
            ActiveSession = activeSession != null,
            ProgressPercent = progressPercent,
            CurrentNoteId = recentNote?.Id,
            FocusState = activeSession?.Status.ToString()
        });
    }

    private static RemoteActionDto MapToDto(RemoteAction action)
    {
        return new RemoteActionDto
        {
            Id = action.Id,
            UserId = action.UserId,
            Kind = action.Kind.ToString(),
            Payload = action.GetPayload(),
            CreatedAt = action.CreatedAt,
            CompletedAt = action.CompletedAt,
            Success = action.Success,
            ErrorMessage = action.ErrorMessage,
            IsCompleted = action.IsCompleted,
            IsPending = action.IsPending
        };
    }
}
