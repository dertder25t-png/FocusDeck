using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System;

namespace FocusDeck.Server.Services.Auth;

internal static class AuthTelemetry
{
    private const string MeterName = "FocusDeck.Authentication";
    private const string MeterVersion = "1.0";
    private static readonly Meter Meter = new(MeterName, MeterVersion);

    private static readonly Counter<long> RegisterSuccessCounter = Meter.CreateCounter<long>("auth.pake.register.success");
    private static readonly Counter<long> RegisterFailureCounter = Meter.CreateCounter<long>("auth.pake.register.failure");
    private static readonly Counter<long> LoginSuccessCounter = Meter.CreateCounter<long>("auth.pake.login.success");
    private static readonly Counter<long> LoginFailureCounter = Meter.CreateCounter<long>("auth.pake.login.failure");
    private static readonly Counter<long> JwtValidationFailureCounter = Meter.CreateCounter<long>("auth.jwt.validation.failure");

    public static void RecordRegisterSuccess(Guid? tenantId = null)
        => RegisterSuccessCounter.Add(1, BuildTags(tenantId));

    public static void RecordRegisterFailure(string reason, Guid? tenantId = null)
        => RegisterFailureCounter.Add(1, BuildTags(tenantId, reason));

    public static void RecordLoginSuccess(Guid? tenantId)
        => LoginSuccessCounter.Add(1, BuildTags(tenantId));

    public static void RecordLoginFailure(string reason, Guid? tenantId = null)
        => LoginFailureCounter.Add(1, BuildTags(tenantId, reason));

    public static void RecordJwtValidationFailure(string reason, Guid? tenantId = null)
        => JwtValidationFailureCounter.Add(1, BuildTags(tenantId, reason));

    public static string MaskIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= 4)
        {
            return new string('*', trimmed.Length);
        }

        return $"{trimmed[..2]}…{trimmed[^2..]}";
    }

    private static KeyValuePair<string, object?>[] BuildTags(Guid? tenantId, string? reason = null)
    {
        var tags = new List<KeyValuePair<string, object?>>
        {
            new("tenant_id", tenantId?.ToString() ?? "unknown")
        };

        if (!string.IsNullOrWhiteSpace(reason))
        {
            tags.Add(new("reason", reason));
        }

        return tags.ToArray();
    }
}
