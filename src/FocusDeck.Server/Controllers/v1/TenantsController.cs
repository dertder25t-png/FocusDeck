using Asp.Versioning;
using FocusDeck.Contracts.MultiTenancy;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Auth;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FocusDeck.Server.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/tenants")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<TenantsController> _logger;
    private readonly ITokenService _tokenService;
    private readonly ICurrentTenant _currentTenant;

    public TenantsController(
        AutomationDbContext db,
        ILogger<TenantsController> logger,
        ITokenService tokenService,
        ICurrentTenant currentTenant)
    {
        _db = db;
        _logger = logger;
        _tokenService = tokenService;
        _currentTenant = currentTenant;
    }

    private string GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> ListTenants()
    {
        var userId = GetUserId();

        var tenants = await _db.UserTenants
            .Include(ut => ut.Tenant)
            .ThenInclude(t => t.Members)
            .Where(ut => ut.UserId == userId)
            .Select(ut => new TenantDto(
                ut.Tenant.Id,
                ut.Tenant.Name,
                ut.Tenant.Slug,
                ut.Tenant.CreatedAt,
                ut.Tenant.Members.Count,
                ut.Role.ToString()
            ))
            .ToListAsync();

        return Ok(tenants);
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentTenantSummary()
    {
        var userId = GetUserId();
        var tenantId = _currentTenant.TenantId;

        if (tenantId == null || tenantId == Guid.Empty)
        {
            tenantId = await _db.UserTenants
                .Where(ut => ut.UserId == userId)
                .OrderBy(ut => ut.JoinedAt)
                .Select(ut => (Guid?)ut.TenantId)
                .FirstOrDefaultAsync();

            if (tenantId == null)
            {
                return NotFound(new { code = "TENANT_NOT_FOUND", message = "No tenant associated with this user." });
            }

            _currentTenant.SetTenant(tenantId.Value);
        }

        var tenant = await _db.Tenants
            .Include(t => t.Members)
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value);

        if (tenant == null)
        {
            return NotFound(new { code = "TENANT_NOT_FOUND", message = "Tenant referenced in token no longer exists." });
        }

        var membership = tenant.Members.FirstOrDefault(m => m.UserId == userId);
        if (membership == null)
        {
            return Forbid();
        }

        return Ok(new CurrentTenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            membership.Role.ToString(),
            tenant.Members.Count
        ));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request)
    {
        var userId = GetUserId();

        if (!IsValidSlug(request.Slug))
        {
            return BadRequest(new { code = "INVALID_SLUG", message = "Slug must contain only lowercase letters, numbers, and hyphens" });
        }

        if (await _db.Tenants.AnyAsync(t => t.Slug == request.Slug))
        {
            return Conflict(new { code = "SLUG_TAKEN", message = "This slug is already in use" });
        }

        var user = await _db.TenantUsers.FindAsync(userId);
        if (user == null)
        {
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? userId;
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

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug,
            CreatedAt = DateTime.UtcNow
        };
        _db.Tenants.Add(tenant);

        var membership = new UserTenant
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = userId,
            Role = TenantRole.Owner,
            JoinedAt = DateTime.UtcNow
        };
        _db.UserTenants.Add(membership);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Tenant {TenantName} created by user {UserId}", tenant.Name, userId);

        return CreatedAtAction(nameof(GetTenant), new { id = tenant.Id }, new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.CreatedAt,
            1,
            TenantRole.Owner.ToString()
        ));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTenant(Guid id)
    {
        var userId = GetUserId();

        var membership = await _db.UserTenants
            .Include(ut => ut.Tenant)
            .ThenInclude(t => t.Members)
            .FirstOrDefaultAsync(ut => ut.TenantId == id && ut.UserId == userId);

        if (membership == null)
        {
            return NotFound(new { code = "TENANT_NOT_FOUND", message = "Tenant not found or access denied" });
        }

        return Ok(new TenantDto(
            membership.Tenant.Id,
            membership.Tenant.Name,
            membership.Tenant.Slug,
            membership.Tenant.CreatedAt,
            membership.Tenant.Members.Count,
            membership.Role.ToString()
        ));
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> ListMembers(Guid id)
    {
        var userId = GetUserId();

        var isMember = await _db.UserTenants.AnyAsync(ut => ut.TenantId == id && ut.UserId == userId);
        if (!isMember)
        {
            return NotFound(new { code = "TENANT_NOT_FOUND", message = "Tenant not found or access denied" });
        }

        var members = await _db.UserTenants
            .Include(ut => ut.User)
            .Where(ut => ut.TenantId == id)
            .Select(ut => new TenantMemberDto(
                ut.UserId,
                ut.User.Email,
                ut.User.Name,
                ut.User.Picture,
                ut.Role.ToString(),
                ut.JoinedAt
            ))
            .ToListAsync();

        return Ok(members);
    }

    [HttpPost("{id}/switch")]
    public async Task<IActionResult> SwitchTenant(Guid id)
    {
        var userId = GetUserId();

        var membership = await _db.UserTenants.FirstOrDefaultAsync(ut => ut.TenantId == id && ut.UserId == userId);
        if (membership == null)
        {
            return NotFound(new { code = "TENANT_NOT_FOUND", message = "Tenant not found or access denied" });
        }

        var accessToken = await _tokenService.GenerateAccessTokenAsync(userId, new[] { membership.Role.ToString() }, id, HttpContext.RequestAborted);
        var refreshToken = _tokenService.GenerateRefreshToken();

        _currentTenant.SetTenant(id);
        _logger.LogInformation("Tenant switch: {UserId} moved to {TenantId}", AuthTelemetry.MaskIdentifier(userId), id);

        return Ok(new { accessToken, refreshToken });
    }

    [HttpDelete("{id}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(Guid id, string memberId)
    {
        var userId = GetUserId();

        var requester = await _db.UserTenants
            .FirstOrDefaultAsync(ut => ut.TenantId == id && ut.UserId == userId);

        if (requester == null || requester.Role == TenantRole.Member)
        {
            return Forbid();
        }

        var member = await _db.UserTenants
            .FirstOrDefaultAsync(ut => ut.TenantId == id && ut.UserId == memberId);

        if (member == null)
        {
            return NotFound(new { code = "MEMBER_NOT_FOUND", message = "Member not found" });
        }

        if (member.Role == TenantRole.Owner)
        {
            var ownerCount = await _db.UserTenants
                .CountAsync(ut => ut.TenantId == id && ut.Role == TenantRole.Owner);

            if (ownerCount <= 1)
            {
                return BadRequest(new { code = "LAST_OWNER", message = "Cannot remove the last owner" });
            }
        }

        _db.UserTenants.Remove(member);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {MemberId} removed from tenant {TenantId} by {UserId}", memberId, id, userId);

        return NoContent();
    }

    private static bool IsValidSlug(string slug)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9-]+$");
    }
}
