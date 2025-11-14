using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Auth;
using FocusDeck.Persistence;
using FocusDeck.Server.Controllers.v1;
using FocusDeck.Server.Services.Auth;
using FocusDeck.Shared.Contracts.Auth;
using FocusDeck.Shared.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading;
using Xunit;

namespace FocusDeck.Server.Tests;

public class AuthPakeE2ETests
{
    private static AutomationDbContext CreateDb(out SqliteConnection conn)
    {
        conn = new SqliteConnection("Filename=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<AutomationDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new AutomationDbContext(options);
        db.Database.EnsureCreated();
        // Tests set up an in-memory database for speed. Do not force migration here so
        // individual tests can control when migrations run (useful for migration tests).
        return db;
    }

    private static IConfiguration CreateConfig()
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('a', 64),
            ["Jwt:Issuer"] = "FocusDeckDev",
            ["Jwt:Audience"] = "focusdeck-clients",
            ["Jwt:AccessTokenExpirationMinutes"] = "60",
            ["Jwt:RefreshTokenExpirationDays"] = "7"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }

    [Fact]
    public async Task Pake_Register_Login_VaultRoundTrip()
    {
        using var db = CreateDb(out var conn);
        await using var _ = conn; // dispose with db

        var controller = CreateController(db);

        var userId = "user@example.com";
        var password = "CorrectHorseBatteryStaple!";

        // 1) Register start
        var startRes = controller.RegisterStart(new RegisterStartRequest(userId)) as OkObjectResult;
        Assert.NotNull(startRes);
        var startPayload = Assert.IsType<RegisterStartResponse>(startRes!.Value);

        var kdf = JsonSerializer.Deserialize<SrpKdfParameters>(startPayload.KdfParametersJson)!;
        Assert.NotNull(kdf);

        // Derive verifier with returned KDF
        var x = Srp.ComputePrivateKey(kdf, userId, password);
        var v = Srp.ComputeVerifier(x);
        var verifierB64 = Convert.ToBase64String(Srp.ToBigEndian(v));

        var vaultData = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });

        // 2) Register finish (also writes vault)
        var finishRes = await controller.RegisterFinish(new RegisterFinishRequest(
            userId,
            verifierB64,
            startPayload.KdfParametersJson,
            vaultData,
            "{\"kdf\":\"argon2id\"}",
            "AES-256-GCM"));
        var finishOk = Assert.IsType<OkObjectResult>(finishRes);
        var finishPayload = Assert.IsType<RegisterFinishResponse>(finishOk.Value);
        Assert.True(finishPayload.Success);

        // 3) Login start
        var (a, A) = Srp.GenerateClientEphemeral();
        var loginStart = await controller.LoginStart(new LoginStartRequest(
            userId,
            Convert.ToBase64String(Srp.ToBigEndian(A))));
        var loginStartOk = Assert.IsType<OkObjectResult>(loginStart);
        var loginStartPayload = Assert.IsType<LoginStartResponse>(loginStartOk.Value);

        // 4) Compute client proof
        var x2 = loginStartPayload.KdfParametersJson != null
            ? Srp.ComputePrivateKey(JsonSerializer.Deserialize<SrpKdfParameters>(loginStartPayload.KdfParametersJson)!, userId, password)
            : Srp.ComputePrivateKey(Convert.FromBase64String(loginStartPayload.SaltBase64), userId, password);
        var B = Srp.FromBigEndian(Convert.FromBase64String(loginStartPayload.ServerPublicEphemeralBase64));
        var u = Srp.ComputeScramble(A, B);
        var Sc = Srp.ComputeClientSession(B, x2, a, u);
        var Kc = Srp.ComputeSessionKey(Sc);
        var M1 = Srp.ComputeClientProof(A, B, Kc);

        // 5) Login finish
        var loginFinish = await controller.LoginFinish(new LoginFinishRequest(
            userId,
            loginStartPayload.SessionId,
            Convert.ToBase64String(M1)));
        var loginFinishOk = Assert.IsType<OkObjectResult>(loginFinish);
        var loginFinishPayload = Assert.IsType<LoginFinishResponse>(loginFinishOk.Value);
        Assert.True(loginFinishPayload.Success);
        Assert.True(loginFinishPayload.HasVault); // vault was stored at registration
        Assert.False(string.IsNullOrWhiteSpace(loginFinishPayload.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(loginFinishPayload.RefreshToken));
    }

    [Fact]
    public async Task Pake_Login_Uses_Legacy_Kdf_For_PreCutover_Credentials()
    {
        using var db = CreateDb(out var conn);
        await using var _ = conn;

        var controller = CreateController(db);
        var userId = "legacy-user@example.com";
        var password = "OldPassword!123";
        var legacySalt = Convert.ToBase64String(Srp.GenerateSalt());

        var legacyPrivateKey = Srp.ComputePrivateKey(Convert.FromBase64String(legacySalt), userId, password);
        var verifier = Srp.ComputeVerifier(legacyPrivateKey);
        var legacyVerifierB64 = Convert.ToBase64String(Srp.ToBigEndian(verifier));

        var staleKdf = new SrpKdfParameters("argon2id", legacySalt, degreeOfParallelism: 2, iterations: 3, memorySizeKiB: 65536, aad: true);

        db.PakeCredentials.Add(new PakeCredential
        {
            UserId = userId,
            SaltBase64 = legacySalt,
            VerifierBase64 = legacyVerifierB64,
            Algorithm = Srp.Algorithm,
            ModulusHex = Srp.ModulusHex,
            Generator = (int)Srp.G,
            KdfParametersJson = JsonSerializer.Serialize(staleKdf),
            CreatedAt = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var (a, A) = Srp.GenerateClientEphemeral();
        var loginStart = await controller.LoginStart(new LoginStartRequest(
            userId,
            Convert.ToBase64String(Srp.ToBigEndian(A))));
        var loginStartOk = Assert.IsType<OkObjectResult>(loginStart);
        var loginStartPayload = Assert.IsType<LoginStartResponse>(loginStartOk.Value);

        Assert.NotNull(loginStartPayload.KdfParametersJson);
        var normalizedKdf = JsonSerializer.Deserialize<SrpKdfParameters>(loginStartPayload.KdfParametersJson!);
        Assert.NotNull(normalizedKdf);
        Assert.Equal("sha256", normalizedKdf!.Algorithm, System.StringComparer.OrdinalIgnoreCase);

        // Legacy derivation path: use fallback salt bytes
        var xLegacy = Srp.ComputePrivateKey(Convert.FromBase64String(loginStartPayload.SaltBase64), userId, password);
        var B = Srp.FromBigEndian(Convert.FromBase64String(loginStartPayload.ServerPublicEphemeralBase64));
        var u = Srp.ComputeScramble(A, B);
        var Sc = Srp.ComputeClientSession(B, xLegacy, a, u);
        var Kc = Srp.ComputeSessionKey(Sc);
        var M1 = Srp.ComputeClientProof(A, B, Kc);

        var loginFinish = await controller.LoginFinish(new LoginFinishRequest(
            userId,
            loginStartPayload.SessionId,
            Convert.ToBase64String(M1)));
        var loginFinishOk = Assert.IsType<OkObjectResult>(loginFinish);
        var loginFinishPayload = Assert.IsType<LoginFinishResponse>(loginFinishOk.Value);
        Assert.True(loginFinishPayload.Success);
    }

    [Fact]
    public async Task Pake_Login_InvalidProof_ReturnsUnauthorized()
    {
        using var db = CreateDb(out var conn);
        await using var _ = conn;

        var controller = CreateController(db);
        var userId = "invalid-proof@example.com";
        var password = "InvalidProof123!";

        var startResult = controller.RegisterStart(new RegisterStartRequest(userId)) as OkObjectResult;
        Assert.NotNull(startResult);
        var startPayload = Assert.IsType<RegisterStartResponse>(startResult!.Value);
        var kdf = JsonSerializer.Deserialize<SrpKdfParameters>(startPayload.KdfParametersJson)!;
        var verifier = Srp.ComputeVerifier(Srp.ComputePrivateKey(kdf, userId, password));
        var verifierB64 = Convert.ToBase64String(Srp.ToBigEndian(verifier));

        var finishResponse = await controller.RegisterFinish(new RegisterFinishRequest(
            userId,
            verifierB64,
            startPayload.KdfParametersJson,
            null));
        Assert.IsType<OkObjectResult>(finishResponse);

        var (clientSecret, clientPublic) = Srp.GenerateClientEphemeral();
        var loginStart = await controller.LoginStart(new LoginStartRequest(
            userId,
            Convert.ToBase64String(Srp.ToBigEndian(clientPublic))));
        var loginStartOk = Assert.IsType<OkObjectResult>(loginStart);
        var loginStartPayload = Assert.IsType<LoginStartResponse>(loginStartOk.Value);

        var invalidProof = Convert.ToBase64String(new byte[] { 0x05, 0x0A, 0x10, 0x20 });
        var loginFinish = await controller.LoginFinish(new LoginFinishRequest(
            userId,
            loginStartPayload.SessionId,
            invalidProof));

        Assert.IsType<UnauthorizedObjectResult>(loginFinish);
    }

    [Fact]
    public async Task Pake_Login_MissingSalt_ReturnsBadRequest()
    {
        using var db = CreateDb(out var conn);
        await using var _ = conn;

        var controller = CreateController(db);
        var userId = "no-salt@example.com";

        var verifier = Convert.ToBase64String(Srp.ToBigEndian(Srp.ComputeVerifier(Srp.ComputePrivateKey(Convert.FromBase64String(Convert.ToBase64String(Srp.GenerateSalt())), userId, "pw"))));

        // Insert a credential with empty salt to simulate migration edge-case
        db.PakeCredentials.Add(new PakeCredential
        {
            UserId = userId,
            SaltBase64 = string.Empty,
            VerifierBase64 = verifier,
            Algorithm = Srp.Algorithm,
            ModulusHex = Srp.ModulusHex,
            Generator = (int)Srp.G,
            KdfParametersJson = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var (a, A) = Srp.GenerateClientEphemeral();
        var loginStart = await controller.LoginStart(new LoginStartRequest(
            userId,
            Convert.ToBase64String(Srp.ToBigEndian(A))));

        Assert.IsType<BadRequestObjectResult>(loginStart);
        var badReq = Assert.IsType<BadRequestObjectResult>(loginStart);
        var payload = badReq.Value!;
        var errorProp = payload.GetType().GetProperty("error");
        Assert.NotNull(errorProp);
        Assert.Equal("Missing KDF salt", errorProp!.GetValue(payload)?.ToString());
    }

    [Fact]
    public async Task Pake_Login_ReturnsSaltFromKdfWhenCredSaltMissing()
    {
        using var db = CreateDb(out var conn);
        await using var _ = conn;

        var controller = CreateController(db);
        var userId = "kdf-salt@example.com";
        var password = "Password123!";

        var derivedSalt = Convert.ToBase64String(Srp.GenerateSalt());

        var kdf = new SrpKdfParameters("sha256", derivedSalt, degreeOfParallelism: 0, iterations: 1, memorySizeKiB: 0, aad: false);

        // Insert a credential that has an empty `SaltBase64` but the KDF metadata contains the salt.
        db.PakeCredentials.Add(new PakeCredential
        {
            UserId = userId,
            SaltBase64 = string.Empty,
            VerifierBase64 = Convert.ToBase64String(Srp.ToBigEndian(Srp.ComputeVerifier(Srp.ComputePrivateKey(Convert.FromBase64String(derivedSalt), userId, password)))),
            Algorithm = Srp.Algorithm,
            ModulusHex = Srp.ModulusHex,
            Generator = (int)Srp.G,
            KdfParametersJson = JsonSerializer.Serialize(kdf),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var (a, A) = Srp.GenerateClientEphemeral();
        var loginStart = await controller.LoginStart(new LoginStartRequest(
            userId,
            Convert.ToBase64String(Srp.ToBigEndian(A))));

        var okRes = Assert.IsType<OkObjectResult>(loginStart);
        var payload = Assert.IsType<LoginStartResponse>(okRes.Value);

        // Expect the salt to come from the KDF metadata and be present on the response
        Assert.Equal(derivedSalt, payload.SaltBase64, ignoreCase: true);
    }

    private static AuthPakeController CreateController(AutomationDbContext db)
    {
        var logger = NullLogger<AuthPakeController>.Instance;
        var tokenService = new TokenService(CreateConfig(), NullLogger<TokenService>.Instance);
        var srpCache = new SrpSessionCache(new MemoryCache(new MemoryCacheOptions()));
        var limiter = new AuthAttemptLimiter(memoryCache: new MemoryCache(new MemoryCacheOptions()));

        var controller = new AuthPakeController(db, logger, tokenService, srpCache, CreateConfig(), limiter, new StubTenantMembershipService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    [Fact]
    public async Task Pake_BackfillMigration_FillsSaltFromKdfJson()
    {
        using var db = CreateDb(out var conn);
        await using var _ = conn;

        var userId = "migrate-salt@example.com";
        var derivedSalt = Convert.ToBase64String(Srp.GenerateSalt());
        var kdf = new SrpKdfParameters("sha256", derivedSalt, degreeOfParallelism: 0, iterations: 0, memorySizeKiB: 0, aad: false);

        db.PakeCredentials.Add(new PakeCredential
        {
            UserId = userId,
            SaltBase64 = string.Empty,
            VerifierBase64 = "dummy",
            Algorithm = Srp.Algorithm,
            ModulusHex = Srp.ModulusHex,
            Generator = (int)Srp.G,
            KdfParametersJson = JsonSerializer.Serialize(kdf),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Run the same backfill logic the server will use (simulates Startup backfill step)
        var rows = await db.PakeCredentials
            .Where(p => string.IsNullOrWhiteSpace(p.SaltBase64) && !string.IsNullOrWhiteSpace(p.KdfParametersJson))
            .ToListAsync();
        foreach (var row in rows)
        {
            var kdfParams = JsonSerializer.Deserialize<SrpKdfParameters>(row.KdfParametersJson!);
            if (kdfParams != null && !string.IsNullOrWhiteSpace(kdfParams.SaltBase64))
            {
                row.SaltBase64 = kdfParams.SaltBase64;
            }
        }
        await db.SaveChangesAsync();

        // Re-query and assert the SaltBase64 was populated by the backfill logic
        var cred = await db.PakeCredentials.FirstOrDefaultAsync(p => p.UserId == userId);
        Assert.NotNull(cred);
        Assert.False(string.IsNullOrWhiteSpace(cred!.SaltBase64));
        Assert.Equal(derivedSalt, cred.SaltBase64);
    }
}

internal sealed class StubTenantMembershipService : FocusDeck.Server.Services.Tenancy.ITenantMembershipService
{
    public Task<Guid> EnsureTenantAsync(string userId, string? email, string? displayName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Guid.NewGuid());
    }
}
