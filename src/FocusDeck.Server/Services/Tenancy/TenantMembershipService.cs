using System;
using System.Text.RegularExpressions;
using System.Threading;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Services.Tenancy;

public interface ITenantMembershipService
{
    Task<Guid> EnsureTenantAsync(string userId, string? email, string? displayName, CancellationToken cancellationToken = default);
}

public class TenantMembershipService : ITenantMembershipService
{
    private readonly AutomationDbContext _db;
    private readonly ILogger<TenantMembershipService> _logger;

    public TenantMembershipService(AutomationDbContext db, ILogger<TenantMembershipService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Guid> EnsureTenantAsync(string userId, string? email, string? displayName, CancellationToken cancellationToken = default)
    {
        var membership = await _db.UserTenants
            .Include(ut => ut.User)
            .FirstOrDefaultAsync(ut => ut.UserId == userId, cancellationToken);

        var maskedUserId = AuthTelemetry.MaskIdentifier(userId);
        if (membership != null)
        {
            _logger.LogInformation("Reusing tenant {TenantId} for {UserId}", membership.TenantId, maskedUserId);
            if (membership.User != null)
            {
                membership.User.LastLoginAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    membership.User.Email = email;
                }

                if (!string.IsNullOrWhiteSpace(displayName))
                {
                    membership.User.Name = displayName;
                }
            }

            return membership.TenantId;
        }

        var tenantUser = await _db.TenantUsers.FindAsync(new object?[] { userId }, cancellationToken);
        if (tenantUser == null)
        {
            tenantUser = new TenantUser
            {
                Id = userId,
                Email = email ?? userId,
                Name = displayName ?? email ?? userId,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            await _db.TenantUsers.AddAsync(tenantUser, cancellationToken);
        }
        else
        {
            tenantUser.LastLoginAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(email))
            {
                tenantUser.Email = email;
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                tenantUser.Name = displayName;
            }
        }

        var tenantName = displayName ?? $"{(email ?? userId)}'s Space";
        var slug = await GenerateUniqueTenantSlugAsync(displayName ?? email ?? userId, cancellationToken);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = tenantName,
            Slug = slug,
            CreatedAt = DateTime.UtcNow
        };
        await _db.Tenants.AddAsync(tenant, cancellationToken);

        var userTenant = new UserTenant
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = userId,
            Role = TenantRole.Owner,
            JoinedAt = DateTime.UtcNow
        };
        await _db.UserTenants.AddAsync(userTenant, cancellationToken);
        _logger.LogInformation("Created tenant {TenantId} for {UserId}", tenant.Id, maskedUserId);

        return tenant.Id;
    }

    private async Task<string> GenerateUniqueTenantSlugAsync(string? rawValue, CancellationToken cancellationToken)
    {
        var value = string.IsNullOrWhiteSpace(rawValue) ? $"tenant-{Guid.NewGuid():N}" : rawValue.ToLowerInvariant();
        var baseSlug = Regex.Replace(value, "[^a-z0-9]+", "-").Trim('-');

        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = $"tenant-{Guid.NewGuid():N}";
        }

        var slug = baseSlug;
        var suffix = 1;
        while (await _db.Tenants.AnyAsync(t => t.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug.Length <= 100 ? slug : slug[..100];
    }
}
