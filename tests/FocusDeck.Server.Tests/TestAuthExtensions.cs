using System;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FocusDeck.Server.Tests;

internal static class TestAuthExtensions
{
    private const string DefaultTenantId = "FD86A760-06C6-4310-BEBB-4B2DC33295C6";
    private static bool _contentRootSet;

    public static HttpClient CreateAuthenticatedClient<TEntryPoint>(this WebApplicationFactory<TEntryPoint> factory, string? tenantId = null)
        where TEntryPoint : class
    {
        EnsureContentRoot();
        var client = factory.CreateClient();
        var token = factory.CreateJwtToken(tenantId ?? DefaultTenantId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string CreateJwtToken<TEntryPoint>(this WebApplicationFactory<TEntryPoint> factory, string tenantId)
        where TEntryPoint : class
    {
        var config = factory.Services.GetRequiredService<IConfiguration>();
        var jwtKey = config.GetValue<string>("Jwt:Key") ?? "test-key-for-testing-purposes-min-32-chars-long";
        var issuer = config.GetValue<string>("Jwt:Issuer") ?? "FocusDeckDev";
        var audience = config.GetValue<string>("Jwt:Audience") ?? "FocusDeckClients";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim("app_tenant_id", tenantId)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
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
