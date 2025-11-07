using Asp.Versioning;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/integrations")]
public class IntegrationsController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly CanvasService _canvas;
    private readonly ILogger<IntegrationsController> _logger;

    public IntegrationsController(AutomationDbContext db, CanvasService canvas, ILogger<IntegrationsController> logger)
    {
        _db = db;
        _canvas = canvas;
        _logger = logger;
    }

    [HttpPost("canvas/refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshCanvas(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Try to find a ConnectedService for this user
        var svc = await _db.ConnectedServices.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Service.ToString() == "Canvas", ct);

        if (svc == null)
        {
            return NotFound(new { error = "Canvas not connected" });
        }

        string? domain = null;
        if (!string.IsNullOrEmpty(svc.MetadataJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(svc.MetadataJson);
                domain = doc.RootElement.TryGetProperty("domain", out var d) ? d.GetString() : null;
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(domain))
        {
            return BadRequest(new { error = "Canvas domain missing in connection metadata" });
        }

        var token = svc.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { error = "Canvas access token missing in connection" });
        }

        try
        {
            var items = await _canvas.GetUpcomingAssignments(domain!, token!);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Canvas refresh failed for user {UserId}", userId);
            return StatusCode(500, new { error = "Canvas refresh failed" });
        }
    }
}

