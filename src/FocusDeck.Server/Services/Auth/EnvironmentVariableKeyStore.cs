using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Configuration;

namespace FocusDeck.Server.Services.Auth;

public sealed class EnvironmentVariableKeyStore : ICryptographicKeyStore
{
    // Support both ASP.NET Core config binding format (JWT__PrimaryKey) and explicit env var format (JWT_PRIMARY_KEY)
    private static readonly string[] PrimaryKeyEnvVars = { "JWT__PrimaryKey", "JWT_PRIMARY_KEY" };
    private static readonly string[] SecondaryKeyEnvVars = { "JWT__SecondaryKey", "JWT_SECONDARY_KEY", "JWT__FallbackSigningKey" };
    private readonly JwtSettings _settings;

    public EnvironmentVariableKeyStore(JwtSettings settings)
    {
        _settings = settings;
    }

    public Task<string> GetPrimaryKeyAsync(CancellationToken ct = default)
    {
        // Try each env var name
        string? key = null;
        foreach (var envVar in PrimaryKeyEnvVars)
        {
            key = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(key))
                break;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            key = _settings.PrimaryKey;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException($"Environment variables ({string.Join(", ", PrimaryKeyEnvVars)}) are missing and JwtSettings.PrimaryKey is not configured.");
        }

        return Task.FromResult(key);
    }

    public Task<string?> GetSecondaryKeyAsync(CancellationToken ct = default)
    {
        // Try each env var name
        string? key = null;
        foreach (var envVar in SecondaryKeyEnvVars)
        {
            key = Environment.GetEnvironmentVariable(envVar);
            if (!string.IsNullOrWhiteSpace(key))
                break;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            key = _settings.SecondaryKey;
        }

        return Task.FromResult(string.IsNullOrWhiteSpace(key) ? null : key);
    }

    public Task RotateKeyAsync(string newPrimaryKey, CancellationToken ct = default)
    {
        // Use the first (preferred) secondary key env var name
        Environment.SetEnvironmentVariable(SecondaryKeyEnvVars[0], newPrimaryKey);
        return Task.CompletedTask;
    }
}
