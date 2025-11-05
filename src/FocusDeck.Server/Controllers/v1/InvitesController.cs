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
[Route("v{version:apiVersion}/invites")]
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

    [HttpPost]
    public async Task<IActionResult> CreateInvite([FromBody] CreateInviteRequest request, [FromQuery] Guid orgId)
    {
        var userId = GetUserId();
        
        // Check if requester is admin or owner
        var requester = await _db.OrgUsers
            .FirstOrDefaultAsync(ou => ou.OrganizationId == orgId && ou.UserId == userId);
            
        if (requester == null || requester.Role == OrgRole.Member)
        {
            return Forbid();
        }
        
        // Validate role
        if (!Enum.TryParse<OrgRole>(request.Role, true, out var role))
        {
            return BadRequest(new { code = "INVALID_ROLE", message = "Invalid role specified" });
        }
        
        // Check if user is already a member
        var existingMember = await _db.OrgUsers
            .AnyAsync(ou => ou.OrganizationId == orgId && ou.User.Email == request.Email);
            
        if (existingMember)
        {
            return Conflict(new { code = "ALREADY_MEMBER", message = "User is already a member of this organization" });
        }
        
        // Check for pending invite
        var existingInvite = await _db.Invites
            .FirstOrDefaultAsync(i => i.OrganizationId == orgId && i.Email == request.Email && i.AcceptedAt == null && i.ExpiresAt > DateTime.UtcNow);
            
        if (existingInvite != null)
        {
            return Conflict(new { code = "INVITE_PENDING", message = "An invitation is already pending for this email" });
        }
        
        // Create invite
        var invite = new Invite
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Email = request.Email,
            Role = role,
            Token = GenerateSecureToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        
        _db.Invites.Add(invite);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Invite created for {Email} to org {OrgId} by {UserId}", request.Email, orgId, userId);
        
        return CreatedAtAction(nameof(GetInvite), new { id = invite.Id }, new InviteDto(
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
        var invite = await _db.Invites.FindAsync(id);
        
        if (invite == null)
        {
            return NotFound(new { code = "INVITE_NOT_FOUND", message = "Invite not found" });
        }
        
        return Ok(new InviteDto(
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
    public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
    {
        var userId = GetUserId();
        var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? userId;
        
        var invite = await _db.Invites
            .Include(i => i.Organization)
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
        
        // Email must match (case-insensitive)
        if (!string.Equals(invite.Email, userEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }
        
        // Check if user already member
        var existingMember = await _db.OrgUsers
            .AnyAsync(ou => ou.OrganizationId == invite.OrganizationId && ou.UserId == userId);
            
        if (existingMember)
        {
            return BadRequest(new { code = "ALREADY_MEMBER", message = "You are already a member of this organization" });
        }
        
        // Ensure user exists
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? userEmail;
            user = new User
            {
                Id = userId,
                Email = userEmail,
                Name = userName,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
        }
        
        // Add user to organization
        var orgUser = new OrgUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = invite.OrganizationId,
            UserId = userId,
            Role = invite.Role,
            JoinedAt = DateTime.UtcNow
        };
        _db.OrgUsers.Add(orgUser);
        
        // Mark invite as accepted
        invite.AcceptedAt = DateTime.UtcNow;
        invite.AcceptedByUserId = userId;
        
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("User {UserId} accepted invite {InviteId} to org {OrgId}", userId, invite.Id, invite.OrganizationId);
        
        return Ok(new { organizationId = invite.OrganizationId, organizationName = invite.Organization.Name });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> RevokeInvite(Guid id, [FromQuery] Guid orgId)
    {
        var userId = GetUserId();
        
        // Check if requester is admin or owner
        var requester = await _db.OrgUsers
            .FirstOrDefaultAsync(ou => ou.OrganizationId == orgId && ou.UserId == userId);
            
        if (requester == null || requester.Role == OrgRole.Member)
        {
            return Forbid();
        }
        
        var invite = await _db.Invites.FindAsync(id);
        if (invite == null || invite.OrganizationId != orgId)
        {
            return NotFound(new { code = "INVITE_NOT_FOUND", message = "Invite not found" });
        }
        
        _db.Invites.Remove(invite);
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
