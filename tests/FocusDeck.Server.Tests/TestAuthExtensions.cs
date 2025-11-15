using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using FocusDeck.Server.Services.Auth;

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

        var client = factory.CreateClient();
        var token = factory.CreateJwtToken(userId, resolvedTenantId);
        var tokenValidationParameters = factory.Services.GetRequiredService<TokenValidationParameters>();
        new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out _);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string CreateJwtToken<TEntryPoint>(this WebApplicationFactory<TEntryPoint> factory, string userId, Guid tenantId)
        where TEntryPoint : class
    {
        using var scope = factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        return tokenService.GenerateAccessToken(userId, new[] { "User" }, tenantId);
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
