using System;
using System.Threading;
using System.Threading.Tasks;
using FocusDeck.Server.Configuration;

namespace FocusDeck.Server.Services.Auth;

public sealed class EnvironmentVariableKeyStore : ICryptographicKeyStore
{
    private const string PrimaryKeyEnvVar = "JWT_PRIMARY_KEY";
    private const string SecondaryKeyEnvVar = "JWT_SECONDARY_KEY";
    private readonly JwtSettings _settings;

    public EnvironmentVariableKeyStore(JwtSettings settings)
    {
        _settings = settings;
    }

    public Task<string> GetPrimaryKeyAsync(CancellationToken ct = default)
    {
        var key = Environment.GetEnvironmentVariable(PrimaryKeyEnvVar);
        if (string.IsNullOrWhiteSpace(key))
        {
            key = _settings.PrimaryKey;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException($"Environment variable '{PrimaryKeyEnvVar}' is missing and JwtSettings.PrimaryKey is not configured.");
        }

        return Task.FromResult(key);
    }

    public Task<string?> GetSecondaryKeyAsync(CancellationToken ct = default)
    {
        var key = Environment.GetEnvironmentVariable(SecondaryKeyEnvVar);
        if (string.IsNullOrWhiteSpace(key))
        {
            key = _settings.SecondaryKey;
        }

        return Task.FromResult(string.IsNullOrWhiteSpace(key) ? null : key);
    }

    public Task RotateKeyAsync(string newPrimaryKey, CancellationToken ct = default)
    {
        Environment.SetEnvironmentVariable(SecondaryKeyEnvVar, newPrimaryKey);
        return Task.CompletedTask;
    }
}
