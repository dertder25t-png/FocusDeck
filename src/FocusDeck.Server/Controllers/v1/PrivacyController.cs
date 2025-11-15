using System.Security.Claims;
using Asp.Versioning;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Server.Services.Privacy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/privacy")]
[Authorize]
public sealed class PrivacyController : ControllerBase
{
    private readonly IPrivacyService _privacyService;
    private readonly ILogger<PrivacyController> _logger;

    public PrivacyController(IPrivacyService privacyService, ILogger<PrivacyController> logger)
    {
        _privacyService = privacyService;
        _logger = logger;
    }

    private string GetUserId()
    {
        var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User?.FindFirst("sub")?.Value
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Privacy controller invoked without an authenticated user");
        }

        return userId;
    }

    [HttpGet("consent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConsent(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("GetConsent called without valid user identifier. Claims: {Claims}", 
                string.Join(", ", User?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));
            return Unauthorized(new { error = "User identifier missing from authentication token" });
        }

        var settings = await _privacyService.GetSettingsAsync(userId, cancellationToken);
        return Ok(settings);
    }

    [HttpPost("consent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateConsent([FromBody] PrivacySettingUpdateDto? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.ContextType))
        {
            return BadRequest(new { error = "ContextType is required." });
        }

        var userId = GetUserId();
        
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("UpdateConsent called without valid user identifier. Claims: {Claims}", 
                string.Join(", ", User?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));
            return Unauthorized(new { error = "User identifier missing from authentication token" });
        }

        try
        {
            var updated = await _privacyService.UpdateSettingAsync(userId, request, cancellationToken);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
