using System;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FocusDeck.Server.Tests;

internal static class TestTenancy
{
    public static readonly Guid DefaultTenantId = Guid.Parse("FD86A760-06C6-4310-BEBB-4B2DC33295C6");
    public const string DefaultUserId = "test-user";

    public static async Task EnsureTenantMembershipAsync(IServiceProvider services, Guid tenantId, string userId)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

        if (await db.UserTenants.AnyAsync(ut => ut.TenantId == tenantId && ut.UserId == userId))
        {
            return;
        }

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
        {
            var slug = $"test-{tenantId:N}".ToLowerInvariant();
            tenant = new Tenant
            {
                Id = tenantId,
                Name = "Test Tenant",
                Slug = slug.Length <= 100 ? slug : slug[..100],
                CreatedAt = DateTime.UtcNow
            };
            await db.Tenants.AddAsync(tenant);
        }

        var user = await db.TenantUsers.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            user = new TenantUser
            {
                Id = userId,
                Email = $"{userId}@focusdeck.test",
                Name = userId,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            await db.TenantUsers.AddAsync(user);
        }
        else
        {
            user.LastLoginAt = DateTime.UtcNow;
        }

        var membership = new UserTenant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Role = TenantRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        await db.UserTenants.AddAsync(membership);
        await db.SaveChangesAsync();
    }
}
