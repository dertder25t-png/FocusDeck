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
[Route("v{version:apiVersion}/orgs")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<OrganizationsController> _logger;

    public OrganizationsController(AutomationDbContext db, ILogger<OrganizationsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> ListOrganizations()
    {
        var userId = GetUserId();
        
        var orgs = await _db.OrgUsers
            .Include(ou => ou.Organization)
            .ThenInclude(o => o.Members)
            .Where(ou => ou.UserId == userId)
            .Select(ou => new OrganizationDto(
                ou.Organization.Id,
                ou.Organization.Name,
                ou.Organization.Slug,
                ou.Organization.CreatedAt,
                ou.Organization.Members.Count,
                ou.Role.ToString()
            ))
            .ToListAsync();

        return Ok(orgs);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationRequest request)
    {
        var userId = GetUserId();
        
        // Validate slug format
        if (!IsValidSlug(request.Slug))
        {
            return BadRequest(new { code = "INVALID_SLUG", message = "Slug must contain only lowercase letters, numbers, and hyphens" });
        }
        
        // Check if slug is already taken
        if (await _db.Organizations.AnyAsync(o => o.Slug == request.Slug))
        {
            return Conflict(new { code = "SLUG_TAKEN", message = "This slug is already in use" });
        }
        
        // Ensure user exists
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? userId;
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
        
        // Create organization
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug,
            CreatedAt = DateTime.UtcNow
        };
        _db.Organizations.Add(org);
        
        // Add creator as owner
        var orgUser = new OrgUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            UserId = userId,
            Role = OrgRole.Owner,
            JoinedAt = DateTime.UtcNow
        };
        _db.OrgUsers.Add(orgUser);
        
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Organization {OrgName} created by user {UserId}", org.Name, userId);
        
        return CreatedAtAction(nameof(GetOrganization), new { id = org.Id }, new OrganizationDto(
            org.Id,
            org.Name,
            org.Slug,
            org.CreatedAt,
            1,
            OrgRole.Owner.ToString()
        ));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrganization(Guid id)
    {
        var userId = GetUserId();
        
        var orgUser = await _db.OrgUsers
            .Include(ou => ou.Organization)
            .ThenInclude(o => o.Members)
            .FirstOrDefaultAsync(ou => ou.OrganizationId == id && ou.UserId == userId);
            
        if (orgUser == null)
        {
            return NotFound(new { code = "ORG_NOT_FOUND", message = "Organization not found or access denied" });
        }
        
        return Ok(new OrganizationDto(
            orgUser.Organization.Id,
            orgUser.Organization.Name,
            orgUser.Organization.Slug,
            orgUser.Organization.CreatedAt,
            orgUser.Organization.Members.Count,
            orgUser.Role.ToString()
        ));
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> ListMembers(Guid id)
    {
        var userId = GetUserId();
        
        // Check if user is member of org
        var isMember = await _db.OrgUsers.AnyAsync(ou => ou.OrganizationId == id && ou.UserId == userId);
        if (!isMember)
        {
            return NotFound(new { code = "ORG_NOT_FOUND", message = "Organization not found or access denied" });
        }
        
        var members = await _db.OrgUsers
            .Include(ou => ou.User)
            .Where(ou => ou.OrganizationId == id)
            .Select(ou => new OrgMemberDto(
                ou.UserId,
                ou.User.Email,
                ou.User.Name,
                ou.User.Picture,
                ou.Role.ToString(),
                ou.JoinedAt
            ))
            .ToListAsync();
            
        return Ok(members);
    }

    [HttpDelete("{id}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(Guid id, string memberId)
    {
        var userId = GetUserId();
        
        // Check if requester is admin or owner
        var requester = await _db.OrgUsers
            .FirstOrDefaultAsync(ou => ou.OrganizationId == id && ou.UserId == userId);
            
        if (requester == null || requester.Role == OrgRole.Member)
        {
            return Forbid();
        }
        
        // Prevent removing the last owner
        var member = await _db.OrgUsers
            .FirstOrDefaultAsync(ou => ou.OrganizationId == id && ou.UserId == memberId);
            
        if (member == null)
        {
            return NotFound(new { code = "MEMBER_NOT_FOUND", message = "Member not found" });
        }
        
        if (member.Role == OrgRole.Owner)
        {
            var ownerCount = await _db.OrgUsers
                .CountAsync(ou => ou.OrganizationId == id && ou.Role == OrgRole.Owner);
                
            if (ownerCount <= 1)
            {
                return BadRequest(new { code = "LAST_OWNER", message = "Cannot remove the last owner" });
            }
        }
        
        _db.OrgUsers.Remove(member);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("User {MemberId} removed from org {OrgId} by {UserId}", memberId, id, userId);
        
        return NoContent();
    }

    private static bool IsValidSlug(string slug)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9-]+$");
    }
}
