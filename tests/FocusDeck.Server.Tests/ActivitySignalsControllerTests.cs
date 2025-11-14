using System.Security.Claims;
using FocusDeck.Contracts.DTOs;
using FocusDeck.Persistence;
using FocusDeck.Server.Controllers.v1;
using FocusDeck.Server.Services.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FocusDeck.Server.Tests;

public class ActivitySignalsControllerTests
{
    [Fact]
    public async Task PostSignal_PersistsActivityAndReturnsAccepted()
    {
        var tenantId = Guid.NewGuid();
        var userId = "user-activity";

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("app_tenant_id", tenantId.ToString())
        }, authenticationType: "TestAuth"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentTenant = new HttpContextCurrentTenant(accessor);
        currentTenant.SetTenant(tenantId);

        var options = new DbContextOptionsBuilder<AutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AutomationDbContext(options, currentTenant);

        var controller = new ActivitySignalsController(db, NullLogger<ActivitySignalsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        var dto = new ActivitySignalDto(
            "TypingBurst",
            "fast",
            "FocusDeck.WebApp",
            DateTime.UtcNow,
            "{\"intensity\":5}");

        var result = await controller.PostSignal(dto, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.NotNull(accepted.Value);

        var saved = await db.ActivitySignals.SingleAsync();
        Assert.Equal("TypingBurst", saved.SignalType);
        Assert.Equal("fast", saved.SignalValue);
        Assert.Equal("FocusDeck.WebApp", saved.SourceApp);
        Assert.Equal("{\"intensity\":5}", saved.MetadataJson);
        Assert.Equal(userId, saved.UserId);
        Assert.Equal(tenantId, saved.TenantId);
    }
}
