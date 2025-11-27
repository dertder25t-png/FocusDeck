using System.Security.Claims;
using Asp.Versioning;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Privacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/activity/signals")]
[Authorize]
public sealed class ActivitySignalsController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<ActivitySignalsController> _logger;
    private readonly IPrivacyService _privacyService;

    public ActivitySignalsController(
        AutomationDbContext db,
        ILogger<ActivitySignalsController> logger,
        IPrivacyService privacyService)
    {
        _db = db;
        _logger = logger;
        _privacyService = privacyService;
    }

    [HttpPost]
    public async Task<IActionResult> PostSignal([FromBody] ActivitySignalDto dto, CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(new { error = "Signal payload is required." });
        }

        if (string.IsNullOrWhiteSpace(dto.SignalType))
        {
            return BadRequest(new { error = "SignalType is required." });
        }

        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User?.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(new { error = "User identifier missing." });
        }
        var signalType = dto.SignalType.Trim();
        if (!await _privacyService.IsEnabledAsync(userId, signalType, cancellationToken))
        {
            _logger.LogWarning("Activity signal {SignalType} blocked because user {UserId} has not consented", signalType, userId);
            return Forbid();
        }

        var capturedAt = dto.CapturedAtUtc != null && dto.CapturedAtUtc.Value != default
            ? dto.CapturedAtUtc.Value
            : DateTime.UtcNow;

        var signal = new ActivitySignal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SignalType = signalType,
            SignalValue = dto.SignalValue ?? string.Empty,
            SourceApp = dto.SourceApp ?? "unknown",
            MetadataJson = dto.MetadataJson,
            CapturedAtUtc = capturedAt
        };

        _db.ActivitySignals.Add(signal);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activity signal captured: {SignalType} @ {CapturedAt} (User={UserId})", signal.SignalType, signal.CapturedAtUtc, userId);
        return Accepted(new { signal.Id });
    }
}
