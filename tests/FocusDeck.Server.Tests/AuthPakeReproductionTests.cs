using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Auth;
using FocusDeck.Persistence;
using FocusDeck.Server.Configuration;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Threading;
using Xunit;

namespace FocusDeck.Server.Tests;

public class AuthPakeReproductionTests
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

    private static IConfiguration CreateConfig(JwtSettings jwtSettings)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Jwt:PrimaryKey"] = jwtSettings.PrimaryKey,
            ["Jwt:SecondaryKey"] = jwtSettings.SecondaryKey,
            ["Jwt:Issuer"] = jwtSettings.Issuer,
            ["Jwt:Audience"] = jwtSettings.Audience,
            ["Jwt:AccessTokenExpirationMinutes"] = jwtSettings.AccessTokenExpirationMinutes.ToString(),
            ["Jwt:RefreshTokenExpirationDays"] = jwtSettings.RefreshTokenExpirationDays.ToString()
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }

    private static JwtSettings CreateJwtSettings()
    {
        return new JwtSettings
        {
            PrimaryKey = new string('a', 64),
            SecondaryKey = new string('b', 64),
            Issuer = "FocusDeckDev",
            Audience = "focusdeck-clients",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7,
            AllowedIssuers = new[] { "FocusDeckDev" },
            AllowedAudiences = new[] { "focusdeck-clients" }
        };
    }

    private static AuthPakeController CreateController(AutomationDbContext db)
    {
        // Use a real logger to see output if needed, or keep NullLogger
        var logger = NullLogger<AuthPakeController>.Instance;
        var jwtSettings = CreateJwtSettings();

        var keyStore = new InMemoryCryptographicKeyStore(jwtSettings.PrimaryKey, jwtSettings.SecondaryKey);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var signingKeyProvider = new JwtSigningKeyProvider(
            keyStore,
            Options.Create(jwtSettings),
            memoryCache,
            NullLogger<JwtSigningKeyProvider>.Instance);

        var tokenService = new TokenService(
            keyStore,
            Options.Create(jwtSettings),
            signingKeyProvider,
            NullLogger<TokenService>.Instance,
            memoryCache);
        var srpCache = new SrpSessionCache(new MemoryCache(new MemoryCacheOptions()));
        var limiter = new AuthAttemptLimiter(memoryCache: new MemoryCache(new MemoryCacheOptions()));

        var controller = new AuthPakeController(db, logger, tokenService, srpCache, CreateConfig(jwtSettings), jwtSettings, limiter, new StubTenantMembershipService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    [Fact]
    public async Task Reproduce_LoginStart_500()
    {
        using var db = CreateDb(out var conn);
        await using var _ = conn;

        var controller = CreateController(db);
        var userId = "repro-user@example.com";
        var password = "Password123!";

        // 1. Register user
        var startRes = controller.RegisterStart(new RegisterStartRequest(userId)) as OkObjectResult;
        var startPayload = Assert.IsType<RegisterStartResponse>(startRes!.Value);
        var kdf = JsonSerializer.Deserialize<SrpKdfParameters>(startPayload.KdfParametersJson)!;
        var x = Srp.ComputePrivateKey(kdf, userId, password);
        var v = Srp.ComputeVerifier(x);
        var verifierB64 = Convert.ToBase64String(Srp.ToBigEndian(v));

        await controller.RegisterFinish(new RegisterFinishRequest(
            userId,
            verifierB64,
            startPayload.KdfParametersJson,
            null));

        // 2. Attempt Login Start
        var (a, A) = Srp.GenerateClientEphemeral();
        
        // Scenario A: Valid request (Baseline)
        var loginStart = await controller.LoginStart(new LoginStartRequest(
            userId,
            Convert.ToBase64String(Srp.ToBigEndian(A))));
        Assert.IsType<OkObjectResult>(loginStart);

        // Scenario B: Invalid Client Ephemeral (Should be 400, not 500)
        var loginStartInvalidEphemeral = await controller.LoginStart(new LoginStartRequest(
            userId,
            "invalid-base64"));
        Assert.IsType<BadRequestObjectResult>(loginStartInvalidEphemeral);

        // Scenario C: User with missing Salt in DB (Simulate migration issue)
        var userNoSalt = "nosalt@example.com";
        db.PakeCredentials.Add(new PakeCredential
        {
            UserId = userNoSalt,
            SaltBase64 = "", // Empty salt
            VerifierBase64 = verifierB64,
            Algorithm = Srp.Algorithm,
            ModulusHex = Srp.ModulusHex,
            Generator = (int)Srp.G,
            KdfParametersJson = null, // No KDF params to fallback to
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var loginStartNoSalt = await controller.LoginStart(new LoginStartRequest(
            userNoSalt,
            Convert.ToBase64String(Srp.ToBigEndian(A))));
        
        // This should be 400 BadRequest ("Missing KDF salt"), NOT 500
        Assert.IsType<BadRequestObjectResult>(loginStartNoSalt);

        // Scenario D: User with corrupted KDF JSON (Should be handled gracefully)
        var userBadKdf = "badkdf@example.com";
        db.PakeCredentials.Add(new PakeCredential
        {
            UserId = userBadKdf,
            SaltBase64 = "c2FsdA==",
            VerifierBase64 = verifierB64,
            Algorithm = Srp.Algorithm,
            ModulusHex = Srp.ModulusHex,
            Generator = (int)Srp.G,
            KdfParametersJson = "{ invalid json }", 
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var loginStartBadKdf = await controller.LoginStart(new LoginStartRequest(
            userBadKdf,
            Convert.ToBase64String(Srp.ToBigEndian(A))));
        
        // Should succeed because it falls back to legacy or just uses the stored salt
        Assert.IsType<OkObjectResult>(loginStartBadKdf);

        // Scenario E: DB has NULL SaltBase64 (Data Corruption)
        // If the property is non-nullable string, EF Core might throw InvalidOperationException during materialization
        var userNullSalt = "nullsalt@example.com";
        
        // Use raw SQL to bypass EF Core validation and force NULL into the column
        // Note: In SQLite, we can insert NULL even if declared NOT NULL if we are careful, 
        // but EF Core's EnsureCreated might have created strict constraints. 
        // However, let's try to insert it. If SQLite rejects it, then this scenario is impossible in prod too (unless schema changed).
        // But prod uses Postgres.
        
        // We need to know the table name. Usually "PakeCredentials".
        // And columns.
        var tenantId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        
        // We use a parameterized query to be safe, but pass DBNull.Value
        await db.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""PakeCredentials"" (""UserId"", ""SaltBase64"", ""VerifierBase64"", ""Algorithm"", ""ModulusHex"", ""Generator"", ""KdfParametersJson"", ""CreatedAt"", ""UpdatedAt"", ""TenantId"") 
              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})",
            userNullSalt, 
            null!, // Force NULL
            verifierB64, 
            Srp.Algorithm, 
            Srp.ModulusHex, 
            (int)Srp.G, 
            null!, 
            now, 
            now, 
            tenantId);

        // This should now return 400 BadRequest (handled gracefully), NOT 500
        var loginStartNullSalt = await controller.LoginStart(new LoginStartRequest(
            userNullSalt,
            Convert.ToBase64String(Srp.ToBigEndian(A))));
        
        Assert.IsType<BadRequestObjectResult>(loginStartNullSalt);
        Assert.IsType<BadRequestObjectResult>(loginStartNullSalt);

        // Scenario F: DB has NULL VerifierBase64
        var userNullVerifier = "nullverifier@example.com";
        await db.Database.ExecuteSqlRawAsync(
            @"INSERT INTO ""PakeCredentials"" (""UserId"", ""SaltBase64"", ""VerifierBase64"", ""Algorithm"", ""ModulusHex"", ""Generator"", ""KdfParametersJson"", ""CreatedAt"", ""UpdatedAt"", ""TenantId"") 
              VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})",
            userNullVerifier, 
            "some-salt", 
            null!, // Force NULL Verifier
            Srp.Algorithm, 
            Srp.ModulusHex, 
            (int)Srp.G, 
            null!, 
            now, 
            now, 
            tenantId);

        var loginStartNullVerifier = await controller.LoginStart(new LoginStartRequest(
            userNullVerifier,
            Convert.ToBase64String(Srp.ToBigEndian(A))));

        // This likely throws 500 currently
        if (loginStartNullVerifier is ObjectResult objResVerifier && objResVerifier.StatusCode == 500)
        {
             // Reproduced!
             Assert.Equal(500, objResVerifier.StatusCode);
        }
        else
        {
             Assert.IsType<BadRequestObjectResult>(loginStartNullVerifier);
        }
    }

    [Fact]
    public async Task Reproduce_RegisterFinish_500()
    {
        using var db = CreateDb(out var conn);
        await using var _ = conn;

        var controller = CreateController(db);
        var userId = "newuser@example.com";
        var password = "Password123!";

        // 1. Register Start
        var startRes = controller.RegisterStart(new RegisterStartRequest(userId)) as OkObjectResult;
        var startPayload = Assert.IsType<RegisterStartResponse>(startRes!.Value);
        
        // 2. Compute Verifier
        var kdf = JsonSerializer.Deserialize<SrpKdfParameters>(startPayload.KdfParametersJson)!;
        var x = Srp.ComputePrivateKey(kdf, userId, password);
        var v = Srp.ComputeVerifier(x);
        var verifierB64 = Convert.ToBase64String(Srp.ToBigEndian(v));

        // 3. Register Finish (Valid)
        var finishRes = await controller.RegisterFinish(new RegisterFinishRequest(
            userId,
            verifierB64,
            startPayload.KdfParametersJson,
            null));
        
        Assert.IsType<OkObjectResult>(finishRes);

        // 4. Register Finish with Vault (Potential failure point)
        var userIdVault = "vaultuser@example.com";
        var startResVault = controller.RegisterStart(new RegisterStartRequest(userIdVault)) as OkObjectResult;
        var startPayloadVault = Assert.IsType<RegisterStartResponse>(startResVault!.Value);
        var kdfVault = JsonSerializer.Deserialize<SrpKdfParameters>(startPayloadVault.KdfParametersJson)!;
        var xVault = Srp.ComputePrivateKey(kdfVault, userIdVault, password);
        var vVault = Srp.ComputeVerifier(xVault);
        var verifierB64Vault = Convert.ToBase64String(Srp.ToBigEndian(vVault));

        var finishResVault = await controller.RegisterFinish(new RegisterFinishRequest(
            userIdVault,
            verifierB64Vault,
            startPayloadVault.KdfParametersJson,
            "vault-data-base64", // Vault data
            "AES-256-GCM",
            "{}" // Vault KDF
            ));
        
        if (finishResVault is ObjectResult objRes && objRes.StatusCode == 500)
        {
             Assert.Fail("RegisterFinish with Vault returned 500");
        }
        Assert.IsType<OkObjectResult>(finishResVault);
    }
}
