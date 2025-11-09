using Asp.Versioning;
using FocusDeck.Contracts.MultiTenancy;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tenant-invites")]
[Authorize]
public class InvitesController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<InvitesController> _logger;

    public InvitesController(AutomationDbContext db, ILogger<InvitesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> ListInvites([FromQuery] Guid tenantId)
    {
        var userId = GetUserId();

        var isMember = await _db.UserTenants.AnyAsync(ut => ut.TenantId == tenantId && ut.UserId == userId);
        if (!isMember)
        {
            return NotFound(new { code = "TENANT_NOT_FOUND", message = "Tenant not found or access denied" });
        }

        var invites = await _db.TenantInvites
            .Where(i => i.TenantId == tenantId)
            .Select(i => new TenantInviteDto(
                i.Id,
                i.Email,
                i.Role.ToString(),
                i.CreatedAt,
                i.ExpiresAt,
                i.ExpiresAt < DateTime.UtcNow,
                i.AcceptedAt != null))
            .ToListAsync();

        return Ok(invites);
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvite([FromBody] CreateTenantInviteRequest request, [FromQuery] Guid tenantId)
    {
        var userId = GetUserId();

        var requester = await _db.UserTenants
            .FirstOrDefaultAsync(ut => ut.TenantId == tenantId && ut.UserId == userId);

        if (requester == null || requester.Role == TenantRole.Member)
        {
            return Forbid();
        }

        if (!Enum.TryParse<TenantRole>(request.Role, true, out var role))
        {
            return BadRequest(new { code = "INVALID_ROLE", message = "Invalid role specified" });
        }

        var existingMember = await _db.UserTenants
            .AnyAsync(ut => ut.TenantId == tenantId && ut.User.Email == request.Email);

        if (existingMember)
        {
            return Conflict(new { code = "ALREADY_MEMBER", message = "User is already a member of this tenant" });
        }

        var existingInvite = await _db.TenantInvites
            .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Email == request.Email && i.AcceptedAt == null && i.ExpiresAt > DateTime.UtcNow);

        if (existingInvite != null)
        {
            return Conflict(new { code = "INVITE_PENDING", message = "An invitation is already pending for this email" });
        }

        var invite = new TenantInvite
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = request.Email,
            Role = role,
            Token = GenerateSecureToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.TenantInvites.Add(invite);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Invite created for {Email} to tenant {TenantId} by {UserId}", request.Email, tenantId, userId);

        return CreatedAtAction(nameof(GetInvite), new { id = invite.Id }, new TenantInviteDto(
            invite.Id,
            invite.Email,
            invite.Role.ToString(),
            invite.CreatedAt,
            invite.ExpiresAt,
            false,
            false
        ));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetInvite(Guid id)
    {
        var invite = await _db.TenantInvites.FindAsync(id);

        if (invite == null)
        {
            return NotFound(new { code = "INVITE_NOT_FOUND", message = "Invite not found" });
        }

        return Ok(new TenantInviteDto(
            invite.Id,
            invite.Email,
            invite.Role.ToString(),
            invite.CreatedAt,
            invite.ExpiresAt,
            invite.ExpiresAt < DateTime.UtcNow,
            invite.AcceptedAt != null
        ));
    }

    [HttpPost("accept")]
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptTenantInviteRequest request)
    {
        var userId = GetUserId();
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? userId;

        var invite = await _db.TenantInvites
            .Include(i => i.Tenant)
            .FirstOrDefaultAsync(i => i.Token == request.Token);

        if (invite == null)
        {
            return NotFound(new { code = "INVITE_NOT_FOUND", message = "Invalid invitation token" });
        }

        if (invite.AcceptedAt != null)
        {
            return BadRequest(new { code = "ALREADY_ACCEPTED", message = "This invitation has already been accepted" });
        }

        if (invite.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { code = "INVITE_EXPIRED", message = "This invitation has expired" });
        }

        if (!string.Equals(invite.Email, userEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var existingMember = await _db.UserTenants
            .AnyAsync(ut => ut.TenantId == invite.TenantId && ut.UserId == userId);

        if (existingMember)
        {
            return BadRequest(new { code = "ALREADY_MEMBER", message = "You are already a member of this tenant" });
        }

        var user = await _db.TenantUsers.FindAsync(userId);
        if (user == null)
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? userEmail;
            user = new TenantUser
            {
                Id = userId,
                Email = userEmail,
                Name = userName,
                CreatedAt = DateTime.UtcNow
            };
            _db.TenantUsers.Add(user);
        }

        var membership = new UserTenant
        {
            Id = Guid.NewGuid(),
            TenantId = invite.TenantId,
            UserId = userId,
            Role = invite.Role,
            JoinedAt = DateTime.UtcNow
        };
        _db.UserTenants.Add(membership);

        invite.AcceptedAt = DateTime.UtcNow;
        invite.AcceptedByUserId = userId;

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} accepted invite {InviteId} to tenant {TenantId}", userId, invite.Id, invite.TenantId);

        return Ok(new { tenantId = invite.TenantId, tenantName = invite.Tenant.Name });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeInvite(Guid id, [FromQuery] Guid tenantId)
    {
        var userId = GetUserId();

        var requester = await _db.UserTenants
            .FirstOrDefaultAsync(ut => ut.TenantId == tenantId && ut.UserId == userId);

        if (requester == null || requester.Role == TenantRole.Member)
        {
            return Forbid();
        }

        var invite = await _db.TenantInvites.FindAsync(id);
        if (invite == null || invite.TenantId != tenantId)
        {
            return NotFound(new { code = "INVITE_NOT_FOUND", message = "Invite not found" });
        }

        _db.TenantInvites.Remove(invite);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Invite {InviteId} revoked by {UserId}", id, userId);

        return NoContent();
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
