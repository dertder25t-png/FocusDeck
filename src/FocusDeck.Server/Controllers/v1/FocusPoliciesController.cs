using Asp.Versioning;
using FocusDeck.Persistence;
using FocusDeck.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FocusDeck.Server.Controllers.v1;

/// <summary>
/// Controller for managing focus policy templates
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/focus/policies")]
[ApiController]
public class FocusPoliciesController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<FocusPoliciesController> _logger;

    public FocusPoliciesController(
        AutomationDbContext db,
        ILogger<FocusPoliciesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private string GetUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("No authenticated user found. Using test user (development only).");
            return "test-user";
        }

        return userId;
    }

    /// <summary>
    /// Get all policy templates for the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FocusPolicyTemplateDto>>> GetPolicies()
    {
        var userId = GetUserId();

        var policies = await _db.FocusPolicyTemplates
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Ok(policies.Select(MapToDto).ToList());
    }

    /// <summary>
    /// Get a policy template by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FocusPolicyTemplateDto>> GetPolicy(Guid id)
    {
        var userId = GetUserId();

        var policy = await _db.FocusPolicyTemplates
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (policy == null)
        {
            return NotFound(new { error = "Policy template not found" });
        }

        return Ok(MapToDto(policy));
    }

    /// <summary>
    /// Create a new policy template
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FocusPolicyTemplateDto>> CreatePolicy([FromBody] CreateFocusPolicyTemplateDto request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Policy name is required" });
        }

        var policy = new FocusPolicyTemplate
        {
            UserId = userId,
            Name = request.Name,
            Strict = request.Strict,
            AutoBreak = request.AutoBreak,
            AutoDim = request.AutoDim,
            NotifyPhone = request.NotifyPhone,
            TargetDurationMinutes = request.TargetDurationMinutes
        };

        _db.FocusPolicyTemplates.Add(policy);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Policy template created: {PolicyId} for user {UserId}", policy.Id, userId);

        return CreatedAtAction(nameof(GetPolicy), new { id = policy.Id }, MapToDto(policy));
    }

    /// <summary>
    /// Update a policy template
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FocusPolicyTemplateDto>> UpdatePolicy(Guid id, [FromBody] UpdateFocusPolicyTemplateDto request)
    {
        var userId = GetUserId();

        var policy = await _db.FocusPolicyTemplates
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (policy == null)
        {
            return NotFound(new { error = "Policy template not found" });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Policy name is required" });
        }

        policy.Name = request.Name;
        policy.Strict = request.Strict;
        policy.AutoBreak = request.AutoBreak;
        policy.AutoDim = request.AutoDim;
        policy.NotifyPhone = request.NotifyPhone;
        policy.TargetDurationMinutes = request.TargetDurationMinutes;
        policy.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Policy template updated: {PolicyId}", policy.Id);

        return Ok(MapToDto(policy));
    }

    /// <summary>
    /// Delete a policy template
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePolicy(Guid id)
    {
        var userId = GetUserId();

        var policy = await _db.FocusPolicyTemplates
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (policy == null)
        {
            return NotFound(new { error = "Policy template not found" });
        }

        _db.FocusPolicyTemplates.Remove(policy);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Policy template deleted: {PolicyId}", policy.Id);

        return NoContent();
    }

    private static FocusPolicyTemplateDto MapToDto(FocusPolicyTemplate policy)
    {
        return new FocusPolicyTemplateDto
        {
            Id = policy.Id,
            Name = policy.Name,
            Strict = policy.Strict,
            AutoBreak = policy.AutoBreak,
            AutoDim = policy.AutoDim,
            NotifyPhone = policy.NotifyPhone,
            TargetDurationMinutes = policy.TargetDurationMinutes,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt
        };
    }
}

/// <summary>
/// DTO for focus policy template
/// </summary>
public class FocusPolicyTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Strict { get; set; }
    public bool AutoBreak { get; set; }
    public bool AutoDim { get; set; }
    public bool NotifyPhone { get; set; }
    public int? TargetDurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating a policy template
/// </summary>
public class CreateFocusPolicyTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public bool Strict { get; set; }
    public bool AutoBreak { get; set; }
    public bool AutoDim { get; set; }
    public bool NotifyPhone { get; set; }
    public int? TargetDurationMinutes { get; set; }
}

/// <summary>
/// DTO for updating a policy template
/// </summary>
public class UpdateFocusPolicyTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public bool Strict { get; set; }
    public bool AutoBreak { get; set; }
    public bool AutoDim { get; set; }
    public bool NotifyPhone { get; set; }
    public int? TargetDurationMinutes { get; set; }
}
