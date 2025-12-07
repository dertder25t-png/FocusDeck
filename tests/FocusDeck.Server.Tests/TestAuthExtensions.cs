using System;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FocusDeck.Server.Tests;

internal static class TestAuthExtensions
{
    private static bool _contentRootSet;

    public static HttpClient CreateAuthenticatedClient<TEntryPoint>(this WebApplicationFactory<TEntryPoint> factory, string? tenantId = null, string? userId = null)
        where TEntryPoint : class
    {
        EnsureContentRoot();
        userId ??= TestTenancy.DefaultUserId;
        var resolvedTenantId = string.IsNullOrWhiteSpace(tenantId)
            ? TestTenancy.DefaultTenantId
            : Guid.Parse(tenantId);

        TestTenancy.EnsureTenantMembershipAsync(factory.Services, resolvedTenantId, userId)
            .GetAwaiter()
            .GetResult();

        // Create authenticated client using test authentication scheme
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(defaultScheme: "TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "TestScheme", options => { });
            });
        }).CreateClient();

        // Add test auth claims to header for test handler
        client.DefaultRequestHeaders.Add("X-Test-User", userId);
        client.DefaultRequestHeaders.Add("X-Test-Tenant", resolvedTenantId.ToString());

        return client;
    }

    private static void EnsureContentRoot()
    {
        if (_contentRootSet)
        {
            return;
        }

        var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
        var root = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
        var serverRoot = Path.Combine(root, "src", "FocusDeck.Server");
        Environment.SetEnvironmentVariable("ASPNETCORE_CONTENTROOT", serverRoot);
        _contentRootSet = true;
    }
}

// Test authentication handler for integration tests
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder) 
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Context.Request.Headers["X-Test-User"].ToString();
        var tenantId = Context.Request.Headers["X-Test-Tenant"].ToString();

        if (string.IsNullOrEmpty(userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId),
            new Claim("app_tenant_id", tenantId ?? TestTenancy.DefaultTenantId.ToString()),
            new Claim(ClaimTypes.Role, "User")
        };

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
