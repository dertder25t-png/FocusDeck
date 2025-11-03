using Asp.Versioning;
using FocusDeck.Persistence;
using FocusDeck.Domain.Entities.Remote;
using FocusDeck.Contracts.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1;

/// <summary>
/// Controller for managing device registration and links
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/devices")]
[ApiController]
public class DevicesController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(AutomationDbContext db, ILogger<DevicesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Register a new device for remote control
    /// </summary>
    /// <param name="request">Device registration details</param>
    /// <returns>Device ID and token</returns>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RegisterDeviceResponseDto>> RegisterDevice([FromBody] RegisterDeviceDto request)
    {
        // Get user ID from claims (in real implementation, this would come from JWT)
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "test-user";

        // Validate device type
        if (!Enum.TryParse<DeviceType>(request.DeviceType, true, out var deviceType))
        {
            return BadRequest(new { error = $"Invalid device type: {request.DeviceType}. Must be 'Desktop' or 'Phone'." });
        }

        // Create device link
        var deviceLink = new DeviceLink
        {
            UserId = userId,
            DeviceType = deviceType,
            Name = request.Name,
            LastSeenUtc = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        deviceLink.SetCapabilities(request.Capabilities);

        _db.DeviceLinks.Add(deviceLink);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Device registered: {DeviceId} ({DeviceType}) for user {UserId}", 
            deviceLink.Id, deviceType, userId);

        // In a real implementation, generate a proper JWT token
        var token = $"device-token-{deviceLink.Id}";

        return Ok(new RegisterDeviceResponseDto
        {
            DeviceId = deviceLink.Id,
            Token = token
        });
    }

    /// <summary>
    /// Get all devices for the current user
    /// </summary>
    /// <returns>List of device links</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DeviceLinkDto>>> GetDevices()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "test-user";

        var devices = await _db.DeviceLinks
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.LastSeenUtc)
            .ToListAsync();

        var dtos = devices.Select(d => new DeviceLinkDto
        {
            Id = d.Id,
            UserId = d.UserId,
            DeviceType = d.DeviceType.ToString(),
            Name = d.Name,
            Capabilities = d.GetCapabilities(),
            LastSeenUtc = d.LastSeenUtc,
            CreatedAt = d.CreatedAt
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Update device last seen timestamp
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <returns>Success status</returns>
    [HttpPut("{deviceId}/heartbeat")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateHeartbeat(Guid deviceId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "test-user";

        var device = await _db.DeviceLinks
            .FirstOrDefaultAsync(d => d.Id == deviceId && d.UserId == userId);

        if (device == null)
        {
            return NotFound(new { error = "Device not found" });
        }

        device.LastSeenUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }
}
