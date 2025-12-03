using Asp.Versioning;
using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Integrations;
using FocusDeck.Services.Abstractions;
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
    private readonly IEncryptionService _encryptionService;

    public IntegrationsController(AutomationDbContext db, CanvasService canvas, ILogger<IntegrationsController> logger, IEncryptionService encryptionService)
    {
        _db = db;
        _canvas = canvas;
        _logger = logger;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Lists all connected services for the current user.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<ConnectedServiceDto>>> GetIntegrations(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var services = await _db.ConnectedServices
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new ConnectedServiceDto(
                s.Id,
                s.Service.ToString(),
                s.IsConfigured,
                s.ConnectedAt,
                s.MetadataJson))
            .ToListAsync(ct);

        return Ok(services);
    }

    /// <summary>
    /// Connects or updates an integration service manually (e.g. API Key).
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> ConnectService([FromBody] ConnectServiceRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (!Enum.TryParse<ServiceType>(request.ServiceType, true, out var serviceType))
        {
            return BadRequest(new { error = "Invalid service type" });
        }

        var existing = await _db.ConnectedServices
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Service == serviceType, ct);

        var accessToken = !string.IsNullOrEmpty(request.AccessToken) ? _encryptionService.Encrypt(request.AccessToken) : "";
        var refreshToken = !string.IsNullOrEmpty(request.RefreshToken) ? _encryptionService.Encrypt(request.RefreshToken) : "";

        if (existing != null)
        {
            // Update
            if (!string.IsNullOrEmpty(request.AccessToken)) existing.AccessToken = accessToken;
            if (!string.IsNullOrEmpty(request.RefreshToken)) existing.RefreshToken = refreshToken;

            existing.MetadataJson = request.MetadataJson ?? existing.MetadataJson;
            existing.ExpiresAt = request.ExpiresAt;
            existing.IsConfigured = true;
            existing.ConnectedAt = DateTime.UtcNow;
        }
        else
        {
            // Create
            var service = new ConnectedService
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Service = serviceType,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                MetadataJson = request.MetadataJson,
                ExpiresAt = request.ExpiresAt,
                IsConfigured = true,
                ConnectedAt = DateTime.UtcNow
            };
            _db.ConnectedServices.Add(service);
        }

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    /// <summary>
    /// Removes a connected integration.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DisconnectService(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var svc = await _db.ConnectedServices
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);

        if (svc == null) return NotFound();

        _db.ConnectedServices.Remove(svc);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    [HttpPost("canvas/refresh")]
    [Authorize]
    public async Task<IActionResult> RefreshCanvas(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        // Try to find a ConnectedService for this user
        var svc = await _db.ConnectedServices.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Service == ServiceType.Canvas, ct);

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

        var token = !string.IsNullOrEmpty(svc.AccessToken) ? _encryptionService.Decrypt(svc.AccessToken) : "";
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

    // Google / Email Endpoints (Stub Implementation for WebApp)
    [HttpGet("google/messages")]
    [Authorize]
    public async Task<ActionResult> GetGoogleMessages(CancellationToken ct)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // In a real implementation, we would use GoogleAuthService to fetch emails
        // For now, returning a mock list to satisfy the frontend contract until the service is fully wired
        var mockEmails = new List<object>
        {
            new { id = "1", sender = "Google Community Team <googlecommunityteam-noreply@google.com>", subject = "Welcome to your new account", snippet = "Hi there, welcome to Google accounts...", date = DateTime.Now.AddDays(-1).ToString("MMM dd"), isRead = true },
            new { id = "2", sender = "GitHub <noreply@github.com>", subject = "[GitHub] Please verify your device", snippet = "A sign-in attempt was made...", date = DateTime.Now.ToString("HH:mm"), isRead = false }
        };
        return Ok(mockEmails);
    }

    [HttpPost("google/send")]
    [Authorize]
    public async Task<IActionResult> SendGoogleMessage([FromBody] SendEmailRequest request, CancellationToken ct)
    {
        // Stub for sending
        return Ok(new { success = true });
    }

    public record ConnectedServiceDto(
        Guid Id,
        string ServiceType,
        bool IsConfigured,
        DateTime ConnectedAt,
        string? MetadataJson
    );

    public record ConnectServiceRequest(
        string ServiceType,
        string? AccessToken,
        string? RefreshToken,
        string? MetadataJson,
        DateTime? ExpiresAt
    );

    public record SendEmailRequest(string To, string Subject, string Body);
}
