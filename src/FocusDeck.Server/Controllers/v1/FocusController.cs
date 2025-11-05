using Asp.Versioning;
using FocusDeck.Persistence;
using FocusDeck.Domain.Entities;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Server.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1;

/// <summary>
/// Controller for managing focus sessions with smart signals
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/focus")]
[ApiController]
public class FocusController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<FocusController> _logger;
    private readonly IHubContext<NotificationsHub, INotificationClient> _hubContext;
    
    // Constants for distraction detection
    private const int DistractionWindowSeconds = 15;
    private const int RecoverySuggestionWindowMinutes = 10;
    private const int RecoverySuggestionThreshold = 3; // Number of distractions in window to trigger suggestion

    public FocusController(
        AutomationDbContext db,
        ILogger<FocusController> logger,
        IHubContext<NotificationsHub, INotificationClient> hubContext)
    {
        _db = db;
        _logger = logger;
        _hubContext = hubContext;
    }

    /// <summary>
    /// Get current user ID from claims with fallback for testing (development only)
    /// </summary>
    private string GetUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            // TODO: Remove this fallback in production - should require proper authentication
            // This is only for development/testing purposes
            _logger.LogWarning("No authenticated user found. Using test user (development only).");
            return "test-user";
        }

        return userId;
    }

    /// <summary>
    /// Create a new focus session
    /// </summary>
    [HttpPost("sessions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FocusSessionDto>> CreateSession([FromBody] CreateFocusSessionDto request)
    {
        var userId = GetUserId();

        var session = new FocusSession
        {
            UserId = userId,
            StartTime = DateTime.UtcNow,
            Status = FocusSessionStatus.Active,
            Policy = new FocusPolicy
            {
                Strict = request.Policy.Strict,
                AutoBreak = request.Policy.AutoBreak,
                AutoDim = request.Policy.AutoDim,
                NotifyPhone = request.Policy.NotifyPhone
            }
        };

        _db.FocusSessions.Add(session);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Focus session created: {SessionId} for user {UserId}", session.Id, userId);

        return CreatedAtAction(nameof(GetSession), new { id = session.Id }, MapToDto(session));
    }

    /// <summary>
    /// Get all focus sessions for the current user
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FocusSessionDto>>> GetSessions([FromQuery] int? limit = 20)
    {
        var userId = GetUserId();

        var sessions = await _db.FocusSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.StartTime)
            .Take(limit ?? 20)
            .ToListAsync();

        return Ok(sessions.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Get a focus session by ID
    /// </summary>
    [HttpGet("sessions/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FocusSessionDto>> GetSession(Guid id)
    {
        var userId = GetUserId();

        var session = await _db.FocusSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (session == null)
        {
            return NotFound(new { error = "Focus session not found" });
        }

        return Ok(MapToDto(session));
    }

    /// <summary>
    /// Get active focus session for the current user
    /// </summary>
    [HttpGet("sessions/active")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FocusSessionDto>> GetActiveSession()
    {
        var userId = GetUserId();

        var session = await _db.FocusSessions
            .Where(s => s.UserId == userId && s.Status == FocusSessionStatus.Active)
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            return NotFound(new { error = "No active focus session found" });
        }

        return Ok(MapToDto(session));
    }

    /// <summary>
    /// Submit a signal to the active focus session
    /// </summary>
    [HttpPost("signals")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SubmitSignal([FromBody] SubmitSignalDto request)
    {
        var userId = GetUserId();

        // Get active session
        var session = await _db.FocusSessions
            .Where(s => s.UserId == userId && s.Status == FocusSessionStatus.Active)
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            return NotFound(new { error = "No active focus session found" });
        }

        // Validate signal kind
        if (!Enum.TryParse<SignalKind>(request.Kind, true, out var signalKind))
        {
            return BadRequest(new { error = $"Invalid signal kind: {request.Kind}" });
        }

        // Add signal to session
        var signal = new FocusSignal
        {
            DeviceId = request.DeviceId,
            Kind = signalKind,
            Value = request.Value,
            Timestamp = request.Timestamp
        };

        session.Signals.Add(signal);
        session.UpdatedAt = DateTime.UtcNow;

        // Check for distractions if strict mode is enabled
        if (session.Policy.Strict)
        {
            var distraction = CheckForDistraction(session, signal);
            if (distraction != null)
            {
                session.DistractionsCount++;

                // Send distraction notification
                // Note: Clients must join the user group via NotificationsHub.JoinUserGroup(userId)
                await _hubContext.Clients.Group($"user:{userId}")
                    .FocusDistraction(distraction.Reason, distraction.At);

                _logger.LogInformation("Distraction detected in session {SessionId}: {Reason}", 
                    session.Id, distraction.Reason);

                // Check if we should suggest recovery
                await CheckForRecoverySuggestion(session, userId);
            }
        }

        await _db.SaveChangesAsync();

        _logger.LogDebug("Signal recorded for session {SessionId}: {Kind} = {Value}", 
            session.Id, signalKind, request.Value);

        return Ok(new { success = true, distractionsCount = session.DistractionsCount });
    }

    /// <summary>
    /// End the active focus session
    /// </summary>
    [HttpPost("sessions/{id}/end")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FocusSessionDto>> EndSession(Guid id)
    {
        var userId = GetUserId();

        var session = await _db.FocusSessions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (session == null)
        {
            return NotFound(new { error = "Focus session not found" });
        }

        if (session.Status != FocusSessionStatus.Active && session.Status != FocusSessionStatus.Paused)
        {
            return BadRequest(new { error = "Session is not active or paused" });
        }

        session.EndTime = DateTime.UtcNow;
        session.Status = FocusSessionStatus.Completed;
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Focus session ended: {SessionId}", session.Id);

        return Ok(MapToDto(session));
    }

    /// <summary>
    /// Check if the signal indicates a distraction
    /// </summary>
    private FocusDistractionDto? CheckForDistraction(FocusSession session, FocusSignal signal)
    {
        var now = signal.Timestamp;
        var windowStart = now.AddSeconds(-DistractionWindowSeconds);

        // Check for phone-related distractions
        if (signal.Kind == SignalKind.PhoneMotion && signal.Value > 0.5)
        {
            // Check if there's recent phone activity
            var recentPhoneActivity = session.Signals
                .Where(s => s.Timestamp >= windowStart && s.Timestamp <= now)
                .Where(s => s.Kind == SignalKind.PhoneMotion || s.Kind == SignalKind.PhoneScreen)
                .Any(s => s.Value > 0);

            if (recentPhoneActivity)
            {
                return new FocusDistractionDto
                {
                    Reason = "Phone motion detected",
                    At = now
                };
            }
        }

        if (signal.Kind == SignalKind.PhoneScreen && signal.Value > 0)
        {
            return new FocusDistractionDto
            {
                Reason = "Phone screen activated",
                At = now
            };
        }

        return null;
    }

    /// <summary>
    /// Check if we should send a recovery suggestion
    /// </summary>
    private async Task CheckForRecoverySuggestion(FocusSession session, string userId)
    {
        if (!session.Policy.AutoBreak)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var windowStart = now.AddMinutes(-RecoverySuggestionWindowMinutes);

        // Don't send another suggestion if we recently sent one
        if (session.LastRecoverySuggestionAt.HasValue && 
            session.LastRecoverySuggestionAt.Value >= windowStart)
        {
            return;
        }

        // Count recent distractions in the window
        var recentDistractionCount = session.Signals
            .Where(s => s.Timestamp >= windowStart && s.Timestamp <= now)
            .Count(s => (s.Kind == SignalKind.PhoneMotion && s.Value > 0.5) || 
                       (s.Kind == SignalKind.PhoneScreen && s.Value > 0));

        if (recentDistractionCount >= RecoverySuggestionThreshold)
        {
            // Send recovery suggestion
            // Note: Clients must join the user group via NotificationsHub.JoinUserGroup(userId)
            var suggestion = session.Policy.Strict 
                ? "Enable Lock Mode" 
                : "Take 2-min break";

            await _hubContext.Clients.Group($"user:{userId}")
                .FocusRecoverySuggested(suggestion);

            session.LastRecoverySuggestionAt = now;

            _logger.LogInformation("Recovery suggestion sent for session {SessionId}: {Suggestion}", 
                session.Id, suggestion);
        }
    }

    private static FocusSessionDto MapToDto(FocusSession session)
    {
        return new FocusSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            Status = session.Status.ToString(),
            Policy = new FocusPolicyDto
            {
                Strict = session.Policy.Strict,
                AutoBreak = session.Policy.AutoBreak,
                AutoDim = session.Policy.AutoDim,
                NotifyPhone = session.Policy.NotifyPhone
            },
            DistractionsCount = session.DistractionsCount,
            LastRecoverySuggestionAt = session.LastRecoverySuggestionAt,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt
        };
    }
}
