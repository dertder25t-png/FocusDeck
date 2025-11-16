using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FocusDeck.Server.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using FocusDeck.Server.Services.Auth;

namespace FocusDeck.Server.Tests;

internal static class TestAuthExtensions
{
    private static bool _contentRootSet;

    public static HttpClient CreateAuthenticatedClient<TEntryPoint>(this WebApplicationFactory<TEntryPoint> factory,
        string? tenantId = null, string? userId = null)
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
        var token = CreateJwtToken(factory, userId, resolvedTenantId);
        var tokenValidationParameters = factory.Services.GetRequiredService<TokenValidationParameters>();

        var keyProvider = factory.Services.GetRequiredService<IJwtSigningKeyProvider>();
        tokenValidationParameters.IssuerSigningKeys = keyProvider.GetValidationKeys();

        new JwtSecurityTokenHandler().ValidateToken(token, tokenValidationParameters, out _);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string CreateJwtToken<TEntryPoint>(WebApplicationFactory<TEntryPoint> factory, string userId,
        Guid tenantId) where TEntryPoint : class
    {
        using var scope = factory.Services.CreateScope();
        var keyProvider = scope.ServiceProvider.GetRequiredService<IJwtSigningKeyProvider>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;

        var signingKey = keyProvider.GetValidationKeys().First();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("tenant_id", tenantId.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
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
