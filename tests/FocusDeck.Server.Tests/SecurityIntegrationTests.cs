using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FocusDeck.Server.Tests;

public class SecurityIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SecurityIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override configuration for tests
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
                    ["Jwt:Key"] = "test-key-for-testing-purposes-min-32-chars-long",
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                    ["Jwt:AccessTokenExpirationMinutes"] = "60",
                    ["Jwt:RefreshTokenExpirationDays"] = "7",
                    ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                    ["Cors:AllowedOrigins:1"] = "http://localhost:5000",
                    ["Storage:Root"] = Path.GetTempPath()
                });
            });

            // Ensure tests run in Development environment
            builder.UseEnvironment("Development");
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetProtectedEndpoint_WithoutAuth_Returns401()
    {
        // Arrange - no Authorization header

        // Act
        var response = await _client.GetAsync("/v1/uploads/asset");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHealthEndpoint_WithoutAuth_Returns200()
    {
        // Arrange - no Authorization header

        // Act
        var response = await _client.GetAsync("/v1/system/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_FirstUse_ReturnsNewTokens()
    {
        // Arrange - Login first
        var loginResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            username = "testuser",
            password = "testpass",
            clientId = "test-client-1"
        });
        
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);

        // Act - Use refresh token
        var refreshResponse = await _client.PostAsJsonAsync("/v1/auth/refresh", new
        {
            accessToken = loginResult.AccessToken,
            refreshToken = loginResult.RefreshToken,
            clientId = "test-client-1"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshResult = await refreshResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(refreshResult);
        Assert.NotEqual(loginResult.RefreshToken, refreshResult.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_ReuseOldToken_Returns401()
    {
        // Arrange - Login and refresh once
        var loginResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            username = "testuser2",
            password = "testpass",
            clientId = "test-client-2"
        });
        
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);

        var firstRefresh = await _client.PostAsJsonAsync("/v1/auth/refresh", new
        {
            accessToken = loginResult.AccessToken,
            refreshToken = loginResult.RefreshToken,
            clientId = "test-client-2"
        });
        
        firstRefresh.EnsureSuccessStatusCode();

        // Act - Try to reuse the old refresh token (replay attack)
        var replayResponse = await _client.PostAsJsonAsync("/v1/auth/refresh", new
        {
            accessToken = loginResult.AccessToken,
            refreshToken = loginResult.RefreshToken, // Old token
            clientId = "test-client-2"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);
        var error = await replayResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("TOKEN_REUSE", error.Code);
    }

    [Fact]
    public async Task RefreshToken_DifferentClientFingerprint_Returns401()
    {
        // Arrange - Login with one client ID
        var loginResponse = await _client.PostAsJsonAsync("/v1/auth/login", new
        {
            username = "testuser3",
            password = "testpass",
            clientId = "test-client-3"
        });
        
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);

        // Act - Try to refresh with different client ID
        var refreshResponse = await _client.PostAsJsonAsync("/v1/auth/refresh", new
        {
            accessToken = loginResult.AccessToken,
            refreshToken = loginResult.RefreshToken,
            clientId = "different-client-id" // Different client
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
        var error = await refreshResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("FINGERPRINT_MISMATCH", error.Code);
    }

    [Fact]
    public async Task CORS_AllowedOrigin_ReturnsSuccess()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/system/health");
        request.Headers.Add("Origin", "http://localhost:5173");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task CORS_DisallowedOrigin_NoAccessControlHeaders()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/v1/system/health");
        request.Headers.Add("Origin", "http://malicious-site.com");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        // Response should succeed (health endpoint allows all)
        // but CORS headers should NOT be present for disallowed origin
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);
    private record ErrorResponse(string Code, string Message, string TraceId);

    [Fact]
    public async Task Pake_RegisterAndLogin_WithArgon2id_Succeeds()
    {
        // Arrange: User credentials
        var userId = $"testuser-{Guid.NewGuid():N}";
        var password = "password123-!@#";

        // Act I: Registration
        // 1. Start registration to get KDF parameters
        var regStartRequest = new FocusDeck.Shared.Contracts.Auth.RegisterStartRequest(userId);
        var regStartResponse = await _client.PostAsJsonAsync("/v1/auth/pake/register/start", regStartRequest);
        regStartResponse.EnsureSuccessStatusCode();
        var regStartResult = await regStartResponse.Content.ReadFromJsonAsync<FocusDeck.Shared.Contracts.Auth.RegisterStartResponse>();
        Assert.NotNull(regStartResult);
        Assert.False(string.IsNullOrEmpty(regStartResult.KdfParametersJson));

        // 2. Client computes verifier using Argon2id
        var kdfParams = System.Text.Json.JsonSerializer.Deserialize<FocusDeck.Shared.Security.SrpKdfParameters>(regStartResult.KdfParametersJson);
        Assert.NotNull(kdfParams);
        var privateKeyX = FocusDeck.Shared.Security.Srp.ComputePrivateKey(kdfParams, userId, password);
        var verifier = FocusDeck.Shared.Security.Srp.ComputeVerifier(privateKeyX);

        // 3. Finish registration
        var regFinishRequest = new FocusDeck.Shared.Contracts.Auth.RegisterFinishRequest(
            userId,
            Convert.ToBase64String(FocusDeck.Shared.Security.Srp.ToBigEndian(verifier)),
            regStartResult.KdfParametersJson,
            null, null, null);
        var regFinishResponse = await _client.PostAsJsonAsync("/v1/auth/pake/register/finish", regFinishRequest);
        regFinishResponse.EnsureSuccessStatusCode();

        // Act II: Login
        // 1. Client generates ephemeral key
        var (clientSecretA, clientPublicA) = FocusDeck.Shared.Security.Srp.GenerateClientEphemeral();

        // 2. Start login
        var loginStartRequest = new FocusDeck.Shared.Contracts.Auth.LoginStartRequest(userId, Convert.ToBase64String(FocusDeck.Shared.Security.Srp.ToBigEndian(clientPublicA)));
        var loginStartResponse = await _client.PostAsJsonAsync("/v1/auth/pake/login/start", loginStartRequest);
        loginStartResponse.EnsureSuccessStatusCode();
        var loginStartResult = await loginStartResponse.Content.ReadFromJsonAsync<FocusDeck.Shared.Contracts.Auth.LoginStartResponse>();
        Assert.NotNull(loginStartResult);

        // 3. Client computes session key and proof
        var serverPublicB = FocusDeck.Shared.Security.Srp.FromBigEndian(Convert.FromBase64String(loginStartResult.ServerPublicEphemeralBase64));
        var scrambleU = FocusDeck.Shared.Security.Srp.ComputeScramble(clientPublicA, serverPublicB);
        var clientSessionS = FocusDeck.Shared.Security.Srp.ComputeClientSession(serverPublicB, privateKeyX, clientSecretA, scrambleU);
        var sessionKeyK = FocusDeck.Shared.Security.Srp.ComputeSessionKey(clientSessionS);
        var clientProofM1 = FocusDeck.Shared.Security.Srp.ComputeClientProof(clientPublicA, serverPublicB, sessionKeyK);

        // 4. Finish login
        var loginFinishRequest = new FocusDeck.Shared.Contracts.Auth.LoginFinishRequest(userId, loginStartResult.SessionId, Convert.ToBase64String(clientProofM1));
        var loginFinishResponse = await _client.PostAsJsonAsync("/v1/auth/pake/login/finish", loginFinishRequest);
        
        // Assert
        loginFinishResponse.EnsureSuccessStatusCode();
        var loginFinishResult = await loginFinishResponse.Content.ReadFromJsonAsync<FocusDeck.Shared.Contracts.Auth.LoginFinishResponse>();
        Assert.NotNull(loginFinishResult);
        Assert.True(loginFinishResult.Success);
        Assert.False(string.IsNullOrWhiteSpace(loginFinishResult.AccessToken));
    }

    [Fact]
    public async Task Pake_LoginAndUpgrade_ForLegacyUser_Succeeds()
    {
        // Arrange: Seed a legacy user directly in the database
        var userId = $"legacy-user-{Guid.NewGuid():N}";
        var password = "password-legacy-456-!@#";
        var salt = FocusDeck.Shared.Security.Srp.GenerateSalt();

        // 1. Client computes legacy verifier
        var privateKeyX = FocusDeck.Shared.Security.Srp.ComputePrivateKey(salt, userId, password);
        var verifier = FocusDeck.Shared.Security.Srp.ComputeVerifier(privateKeyX);

        // 2. Seed database
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
            dbContext.PakeCredentials.Add(new FocusDeck.Domain.Entities.Auth.PakeCredential
            {
                UserId = userId,
                SaltBase64 = Convert.ToBase64String(salt),
                VerifierBase64 = Convert.ToBase64String(FocusDeck.Shared.Security.Srp.ToBigEndian(verifier)),
                Algorithm = FocusDeck.Shared.Security.Srp.Algorithm,
                ModulusHex = FocusDeck.Shared.Security.Srp.ModulusHex,
                Generator = (int)FocusDeck.Shared.Security.Srp.G,
                KdfParametersJson = null, // Explicitly null for legacy user
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        // Act I: Legacy Login
        var (clientSecretA, clientPublicA) = FocusDeck.Shared.Security.Srp.GenerateClientEphemeral();
        var loginStartRequest = new FocusDeck.Shared.Contracts.Auth.LoginStartRequest(userId, Convert.ToBase64String(FocusDeck.Shared.Security.Srp.ToBigEndian(clientPublicA)));
        var loginStartResponse = await _client.PostAsJsonAsync("/v1/auth/pake/login/start", loginStartRequest);
        loginStartResponse.EnsureSuccessStatusCode();
        var loginStartResult = await loginStartResponse.Content.ReadFromJsonAsync<FocusDeck.Shared.Contracts.Auth.LoginStartResponse>();
        Assert.NotNull(loginStartResult);
        Assert.Null(loginStartResult.KdfParametersJson); // Assert it's a legacy login

        var serverPublicB = FocusDeck.Shared.Security.Srp.FromBigEndian(Convert.FromBase64String(loginStartResult.ServerPublicEphemeralBase64));
        var scrambleU = FocusDeck.Shared.Security.Srp.ComputeScramble(clientPublicA, serverPublicB);
        var clientSessionS = FocusDeck.Shared.Security.Srp.ComputeClientSession(serverPublicB, privateKeyX, clientSecretA, scrambleU);
        var sessionKeyK = FocusDeck.Shared.Security.Srp.ComputeSessionKey(clientSessionS);
        var clientProofM1 = FocusDeck.Shared.Security.Srp.ComputeClientProof(clientPublicA, serverPublicB, sessionKeyK);

        var loginFinishRequest = new FocusDeck.Shared.Contracts.Auth.LoginFinishRequest(userId, loginStartResult.SessionId, Convert.ToBase64String(clientProofM1));
        var loginFinishResponse = await _client.PostAsJsonAsync("/v1/auth/pake/login/finish", loginFinishRequest);
        loginFinishResponse.EnsureSuccessStatusCode();
        var loginFinishResult = await loginFinishResponse.Content.ReadFromJsonAsync<FocusDeck.Shared.Contracts.Auth.LoginFinishResponse>();
        Assert.NotNull(loginFinishResult);
        var accessToken = loginFinishResult.AccessToken;

        // Act II: Upgrade Credential
        // 1. Client generates new KDF params and verifier
        var newKdfParams = FocusDeck.Shared.Security.Srp.GenerateKdfParameters();
        var newPrivateKeyX = FocusDeck.Shared.Security.Srp.ComputePrivateKey(newKdfParams, userId, password);
        var newVerifier = FocusDeck.Shared.Security.Srp.ComputeVerifier(newPrivateKeyX);
        var newKdfParamsJson = System.Text.Json.JsonSerializer.Serialize(newKdfParams);

        // 2. Call upgrade endpoint with auth token
        var upgradeRequest = new FocusDeck.Shared.Contracts.Auth.UpgradeCredentialRequest(userId, Convert.ToBase64String(FocusDeck.Shared.Security.Srp.ToBigEndian(newVerifier)), newKdfParamsJson);
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/v1/auth/pake/upgrade");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        requestMessage.Content = JsonContent.Create(upgradeRequest);
        var upgradeResponse = await _client.SendAsync(requestMessage);

        // Assert II: Upgrade was successful
        upgradeResponse.EnsureSuccessStatusCode();

        // 3. Verify in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
            var updatedCred = await dbContext.PakeCredentials.AsNoTracking().FirstOrDefaultAsync(c => c.UserId == userId);
            Assert.NotNull(updatedCred);
            Assert.NotNull(updatedCred.KdfParametersJson);
            Assert.Equal(newKdfParamsJson, updatedCred.KdfParametersJson);
            Assert.Equal(Convert.ToBase64String(FocusDeck.Shared.Security.Srp.ToBigEndian(newVerifier)), updatedCred.VerifierBase64);
        }
    }
}
