using System.Security.Claims;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Privacy;
using FocusDeck.Server.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FocusDeck.Server.Tests.Services.Privacy;

public class PrivacyServiceTests
{
    [Fact]
    public async Task GetSettingsAsync_IncludesContextDefinitions()
    {
        var (service, _) = CreateService("privacy-user");
        var result = await service.GetSettingsAsync("privacy-user", CancellationToken.None);

        Assert.Contains(result, entry => entry.ContextType == "TypingVelocity");
        Assert.Contains(result, entry => entry.ContextType == "ActiveWindowTitle");
    }

    [Fact]
    public async Task UpdateSettingAsync_EnablesContext()
    {
        var (service, _) = CreateService("privacy-user");
        var dto = new PrivacySettingUpdateDto("TypingVelocity", true);

        var updated = await service.UpdateSettingAsync("privacy-user", dto, CancellationToken.None);
        Assert.True(updated.IsEnabled);
        Assert.Equal("TypingVelocity", updated.ContextType);
        Assert.True(await service.IsEnabledAsync("privacy-user", "TypingVelocity", CancellationToken.None));
    }

    private static (PrivacyService Service, AutomationDbContext Db) CreateService(string userId)
    {
        var tenantId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("app_tenant_id", tenantId.ToString())
            }, "TestAuth"))
        };

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentTenant = new HttpContextCurrentTenant(accessor);
        currentTenant.SetTenant(tenantId);

        var options = new DbContextOptionsBuilder<AutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new AutomationDbContext(options, currentTenant);
        var service = new PrivacyService(db, currentTenant, NullLogger<PrivacyService>.Instance);
        return (service, db);
    }
}
