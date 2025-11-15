using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using FocusDeck.Server.Middleware;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FocusDeck.Server.Tests;

public class TenancyMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AllowAnonymousEndpoint_BypassesTenantResolution()
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new AllowAnonymousAttribute()),
            "anon"));

        var middleware = new TenancyMiddleware(_ => Task.CompletedTask, NullLogger<TenancyMiddleware>.Instance);
        var tenant = new TestTenant();

        await middleware.InvokeAsync(context, tenant);

        Assert.False(tenant.HasTenant);
    }

    [Fact]
    public async Task InvokeAsync_MissingTenantClaim_ReturnsForbidden()
    {
        var context = CreateAuthenticatedContext();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute()),
            "auth"));
        context.Response.Body = new MemoryStream();

        var middleware = new TenancyMiddleware(_ => Task.CompletedTask, NullLogger<TenancyMiddleware>.Instance);
        var tenant = new TestTenant();

        await middleware.InvokeAsync(context, tenant);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        Assert.Contains("Tenant context missing", body);
        Assert.False(tenant.HasTenant);
    }

    [Fact]
    public async Task InvokeAsync_TenantClaimPresent_SetsCurrentTenantAndInvokesNext()
    {
        var context = CreateAuthenticatedContext("app_tenant_id", Guid.NewGuid().ToString());
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new AuthorizeAttribute()),
            "auth"));

        var nextInvoked = false;
        var middleware = new TenancyMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        }, NullLogger<TenancyMiddleware>.Instance);

        var tenant = new TestTenant();
        await middleware.InvokeAsync(context, tenant);

        Assert.True(nextInvoked);
        Assert.True(tenant.HasTenant);
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string claimType = ClaimTypes.NameIdentifier, string claimValue = "user")
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(claimType, claimValue)
        }, "TestAuth"));

        return context;
    }

    private sealed class TestTenant : ICurrentTenant
    {
        public Guid? TenantId { get; private set; }
        public bool HasTenant => TenantId.HasValue && TenantId.Value != Guid.Empty;

        public void SetTenant(Guid tenantId)
        {
            TenantId = tenantId;
        }
    }
}
