using System.Numerics;
using System.Text.Json;
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

        var logger = NullLogger<AuthPakeController>.Instance;
        var tokenService = new TokenService(CreateConfig(), NullLogger<TokenService>.Instance);
        var srpCache = new SrpSessionCache(new MemoryCache(new MemoryCacheOptions()));
        var limiter = new AuthAttemptLimiter(memoryCache: new MemoryCache(new MemoryCacheOptions()));

        var controller = new AuthPakeController(db, logger, tokenService, srpCache, CreateConfig(), limiter)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

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
        var salt = Convert.FromBase64String(loginStartPayload.SaltBase64);
        var x2 = Srp.ComputePrivateKey(salt, userId, password);
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
}

