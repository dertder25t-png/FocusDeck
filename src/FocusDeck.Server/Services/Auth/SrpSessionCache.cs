using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;

namespace FocusDeck.Server.Services.Auth;

public interface ISrpSessionCache
{
    SrpServerSession Store(
        string userId,
        byte[] salt,
        BigInteger verifier,
        BigInteger clientPublic,
        BigInteger serverSecret,
        BigInteger serverPublic,
        BigInteger scramble,
        string? clientId,
        string? deviceName,
        string? devicePlatform);
    bool TryGet(Guid sessionId, out SrpServerSession? session);
    void Remove(Guid sessionId);
}

public sealed record SrpServerSession(
    Guid SessionId,
    string UserId,
    byte[] Salt,
    BigInteger Verifier,
    BigInteger ClientPublic,
    BigInteger ServerSecret,
    BigInteger ServerPublic,
    BigInteger Scramble,
    string? ClientId,
    string? DeviceName,
    string? DevicePlatform,
    DateTimeOffset ExpiresAt
);

// Helper DTO for serialization because BigInteger doesn't serialize nicely to JSON by default in all contexts,
// and we want to ensure robust cross-server compatibility.
internal record SrpSessionDto
{
    public Guid SessionId { get; set; }
    public string UserId { get; set; } = "";
    public string SaltBase64 { get; set; } = "";
    public string VerifierBase64 { get; set; } = "";
    public string ClientPublicBase64 { get; set; } = "";
    public string ServerSecretBase64 { get; set; } = "";
    public string ServerPublicBase64 { get; set; } = "";
    public string ScrambleBase64 { get; set; } = "";
    public string? ClientId { get; set; }
    public string? DeviceName { get; set; }
    public string? DevicePlatform { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    public static SrpSessionDto FromDomain(SrpServerSession session) => new()
    {
        SessionId = session.SessionId,
        UserId = session.UserId,
        SaltBase64 = Convert.ToBase64String(session.Salt),
        VerifierBase64 = Convert.ToBase64String(session.Verifier.ToByteArray(isUnsigned: true, isBigEndian: true)),
        ClientPublicBase64 = Convert.ToBase64String(session.ClientPublic.ToByteArray(isUnsigned: true, isBigEndian: true)),
        ServerSecretBase64 = Convert.ToBase64String(session.ServerSecret.ToByteArray(isUnsigned: true, isBigEndian: true)),
        ServerPublicBase64 = Convert.ToBase64String(session.ServerPublic.ToByteArray(isUnsigned: true, isBigEndian: true)),
        ScrambleBase64 = Convert.ToBase64String(session.Scramble.ToByteArray(isUnsigned: true, isBigEndian: true)),
        ClientId = session.ClientId,
        DeviceName = session.DeviceName,
        DevicePlatform = session.DevicePlatform,
        ExpiresAt = session.ExpiresAt
    };

    public SrpServerSession ToDomain() => new(
        SessionId,
        UserId,
        Convert.FromBase64String(SaltBase64),
        new BigInteger(Convert.FromBase64String(VerifierBase64), isUnsigned: true, isBigEndian: true),
        new BigInteger(Convert.FromBase64String(ClientPublicBase64), isUnsigned: true, isBigEndian: true),
        new BigInteger(Convert.FromBase64String(ServerSecretBase64), isUnsigned: true, isBigEndian: true),
        new BigInteger(Convert.FromBase64String(ServerPublicBase64), isUnsigned: true, isBigEndian: true),
        new BigInteger(Convert.FromBase64String(ScrambleBase64), isUnsigned: true, isBigEndian: true),
        ClientId,
        DeviceName,
        DevicePlatform,
        ExpiresAt
    );
}

public sealed class SrpSessionCache : ISrpSessionCache
{
    private readonly IDistributedCache _cache;

    public SrpSessionCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    public SrpServerSession Store(
        string userId,
        byte[] salt,
        BigInteger verifier,
        BigInteger clientPublic,
        BigInteger serverSecret,
        BigInteger serverPublic,
        BigInteger scramble,
        string? clientId,
        string? deviceName,
        string? devicePlatform)
    {
        var sessionId = Guid.NewGuid();
        var record = new SrpServerSession(
            sessionId,
            userId,
            salt,
            verifier,
            clientPublic,
            serverSecret,
            serverPublic,
            scramble,
            clientId,
            deviceName,
            devicePlatform,
            DateTimeOffset.UtcNow.AddMinutes(5));

        var dto = SrpSessionDto.FromDomain(record);
        var json = JsonSerializer.Serialize(dto);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        _cache.SetString(sessionId.ToString(), json, options);
        return record;
    }

    public bool TryGet(Guid sessionId, out SrpServerSession? session)
    {
        var json = _cache.GetString(sessionId.ToString());
        if (string.IsNullOrEmpty(json))
        {
            session = null;
            return false;
        }

        try
        {
            var dto = JsonSerializer.Deserialize<SrpSessionDto>(json);
            if (dto != null && dto.ExpiresAt > DateTimeOffset.UtcNow)
            {
                session = dto.ToDomain();
                return true;
            }
        }
        catch
        {
            // Invalid data in cache, ignore
        }

        session = null;
        return false;
    }

    public void Remove(Guid sessionId)
    {
        _cache.Remove(sessionId.ToString());
    }
}
