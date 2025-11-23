using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Linq;
using FocusDeck.Contracts.MultiTenancy;
using FocusDeck.Shared.Contracts.Auth;
using FocusDeck.Shared.Security;
using Microsoft.Maui.Devices;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Mobile.Services.Auth;

public record MobileAuthResult(string UserId, string AccessToken, string RefreshToken, int ExpiresIn, bool HasVault);

public interface IMobileAuthService
{
    Task<MobileAuthResult?> LoginAsync(string serverBaseUrl, string userId, string password, CancellationToken cancellationToken = default);
    Task<bool> RegisterAsync(string serverBaseUrl, string userId, string password, CancellationToken cancellationToken = default);
    Task LogoutAsync(CancellationToken cancellationToken = default);
    Task<CurrentTenantDto?> RefreshCurrentTenantAsync(CancellationToken cancellationToken = default);
    CurrentTenantDto? CurrentTenant { get; }
    event EventHandler<CurrentTenantDto?>? CurrentTenantChanged;
    Task<bool> RedeemClaimCodeAsync(string scannedData, CancellationToken cancellationToken = default);
}

public class MobilePakeAuthService : IMobileAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IDeviceIdService _deviceIdService;
    private readonly MobileTokenStore _tokenStore;
    private readonly MobileVaultService _vaultService;
    private readonly ILogger<MobilePakeAuthService> _logger;
    private Uri? _lastBaseUri;
    private string? _accessToken;
    private CurrentTenantDto? _currentTenant;

    public MobilePakeAuthService(HttpClient httpClient, IDeviceIdService deviceIdService, MobileTokenStore tokenStore, MobileVaultService vaultService, ILogger<MobilePakeAuthService> logger)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _deviceIdService = deviceIdService;
        _tokenStore = tokenStore;
        _vaultService = vaultService;
        _logger = logger;
    }

    public async Task<bool> RegisterAsync(string serverBaseUrl, string userId, string password, CancellationToken cancellationToken = default)
    {
        var baseUri = BuildBaseUri(serverBaseUrl);
        var startResponse = await PostJsonAsync<RegisterStartRequest, RegisterStartResponse>(
            baseUri,
            "/v1/auth/pake/register/start",
            new RegisterStartRequest(userId),
            cancellationToken);

        if (startResponse == null)
        {
            _logger.LogWarning("RegisterStart response was null for {UserId}", userId);
            return false;
        }

        var kdfParams = System.Text.Json.JsonSerializer.Deserialize<FocusDeck.Shared.Security.SrpKdfParameters>(startResponse.KdfParametersJson);
        if (kdfParams == null)
        {
            _logger.LogWarning("Failed to deserialize KDF parameters for {UserId}", userId);
            return false;
        }

        var privateKey = Srp.ComputePrivateKey(kdfParams, userId, password);
        var verifier = Srp.ComputeVerifier(privateKey);
        var vault = await _vaultService.ExportEncryptedAsync(password);

        var finishRequest = new RegisterFinishRequest(
            userId,
            Convert.ToBase64String(Srp.ToBigEndian(verifier)),
            startResponse.KdfParametersJson,
            vault.CipherText,
            vault.KdfMetadataJson,
            vault.CipherSuite);

        var finishResponse = await PostJsonAsync<RegisterFinishRequest, RegisterFinishResponse>(
            baseUri,
            "/v1/auth/pake/register/finish",
            finishRequest,
            cancellationToken);

        if (finishResponse?.Success != true)
        {
            _logger.LogWarning("RegisterFinish failed for {UserId}", userId);
            return false;
        }

        _logger.LogInformation("Registered new user {UserId} with encrypted vault payload", userId);
        return true;
    }

    public async Task<MobileAuthResult?> LoginAsync(string serverBaseUrl, string userId, string password, CancellationToken cancellationToken = default)
    {
        var baseUri = BuildBaseUri(serverBaseUrl);
        var deviceId = await _deviceIdService.GetDeviceIdAsync();
        var (clientSecret, clientPublic) = Srp.GenerateClientEphemeral();

        var startRequest = new LoginStartRequest(
            userId,
            Convert.ToBase64String(Srp.ToBigEndian(clientPublic)),
            deviceId,
            DeviceInfo.Current.Name,
            DeviceInfo.Current.Platform.ToString());

        var startResponse = await PostJsonAsync<LoginStartRequest, LoginStartResponse>(baseUri, "/v1/auth/pake/login/start", startRequest, cancellationToken);
        if (startResponse == null)
        {
            _logger.LogWarning("LoginStart response was null");
            return null;
        }

        var salt = Convert.FromBase64String(startResponse.SaltBase64);
        var serverPublic = Srp.FromBigEndian(Convert.FromBase64String(startResponse.ServerPublicEphemeralBase64));
        var privateKey = Srp.ComputePrivateKey(salt, userId, password);
        var scramble = Srp.ComputeScramble(clientPublic, serverPublic);
        if (scramble.Sign == 0)
        {
            _logger.LogWarning("SRP scramble parameter was zero");
            return null;
        }

        var sessionSecret = Srp.ComputeClientSession(serverPublic, privateKey, clientSecret, scramble);
        var sessionKey = Srp.ComputeSessionKey(sessionSecret);
        var clientProofBytes = Srp.ComputeClientProof(clientPublic, serverPublic, sessionKey);
        var finishRequest = new LoginFinishRequest(
            userId,
            startResponse.SessionId,
            Convert.ToBase64String(clientProofBytes),
            deviceId,
            DeviceInfo.Current.Name,
            DeviceInfo.Current.Platform.ToString());

        var finishResponse = await PostJsonAsync<LoginFinishRequest, LoginFinishResponse>(baseUri, "/v1/auth/pake/login/finish", finishRequest, cancellationToken);
        if (finishResponse == null || !finishResponse.Success)
        {
            _logger.LogWarning("LoginFinish response was invalid");
            return null;
        }

        var expectedServerProof = Srp.ComputeServerProof(clientPublic, clientProofBytes, sessionKey);
        var actualProof = Convert.FromBase64String(finishResponse.ServerProofBase64);
        if (!actualProof.SequenceEqual(expectedServerProof))
        {
            _logger.LogWarning("Server proof mismatch");
            return null;
        }

        await _tokenStore.SaveAsync(userId, finishResponse.AccessToken, finishResponse.RefreshToken);
        _lastBaseUri = baseUri;
        SetAccessToken(finishResponse.AccessToken);
        await RefreshCurrentTenantAsync(cancellationToken);

        return new MobileAuthResult(
            userId,
            finishResponse.AccessToken,
            finishResponse.RefreshToken,
            finishResponse.ExpiresIn,
            finishResponse.HasVault);
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var userId = await _tokenStore.GetUserIdAsync();
        if (userId == null) return;

        var serverUrl = Preferences.Get("cloud_server_url", "");
        if (string.IsNullOrWhiteSpace(serverUrl)) return;

        var baseUri = BuildBaseUri(serverUrl);
        var requestUri = new Uri(baseUri, "/v1/auth/logout");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            var token = await _tokenStore.GetAccessTokenAsync();
            if (token != null)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Server logout call failed, clearing local tokens anyway.");
        }
        finally
        {
            await _tokenStore.ClearAsync(userId);
            SetAccessToken(null);
            UpdateCurrentTenant(null);
        }
    }

    private static Uri BuildBaseUri(string serverBaseUrl)
    {
        if (Uri.TryCreate(serverBaseUrl, UriKind.Absolute, out var absolute))
        {
            return new Uri(absolute.GetLeftPart(UriPartial.Authority));
        }

        throw new ArgumentException("Invalid server URL", nameof(serverBaseUrl));
    }

    private async Task<TResponse?> PostJsonAsync<TRequest, TResponse>(Uri baseUri, string path, TRequest payload, CancellationToken cancellationToken)
    {
        var requestUri = new Uri(baseUri, path);
        using var response = await _httpClient.PostAsJsonAsync(requestUri, payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Request {Path} failed with status {StatusCode}", path, response.StatusCode);
            return default;
        }
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
    }

    private void SetAccessToken(string? token)
    {
        _accessToken = token;
        if (string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private void UpdateCurrentTenant(CurrentTenantDto? tenant)
    {
        if (tenant?.Id == _currentTenant?.Id) return;
        _currentTenant = tenant;
        CurrentTenantChanged?.Invoke(this, tenant);
    }

    public CurrentTenantDto? CurrentTenant => _currentTenant;

    public event EventHandler<CurrentTenantDto?>? CurrentTenantChanged;

    public async Task<CurrentTenantDto?> RefreshCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        if (_lastBaseUri == null || string.IsNullOrEmpty(_accessToken))
        {
            return null;
        }

        try
        {
            var requestUri = new Uri(_lastBaseUri, "/v1/tenants/current");
            var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Current tenant request failed with {StatusCode}", response.StatusCode);
                UpdateCurrentTenant(null);
                return null;
            }

            var tenant = await response.Content.ReadFromJsonAsync<CurrentTenantDto>(cancellationToken: cancellationToken);
            UpdateCurrentTenant(tenant);
            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh current tenant");
            UpdateCurrentTenant(null);
            return null;
        }
    }

    public async Task<bool> RedeemClaimCodeAsync(string scannedData, CancellationToken cancellationToken = default)
    {
        // Expected format: JSON or pipe-delimited
        // We'll support pipe-delimited for now as it's compact: {pairingId}|{code}|{serverUrl}
        // Or JSON: { "pairingId": "...", "code": "...", "serverUrl": "..." }

        Guid pairingId;
        string code;
        string serverUrl;

        try
        {
            if (scannedData.Trim().StartsWith("{"))
            {
                var json = System.Text.Json.JsonDocument.Parse(scannedData);
                pairingId = Guid.Parse(json.RootElement.GetProperty("pairingId").GetString()!);
                code = json.RootElement.GetProperty("code").GetString()!;
                serverUrl = json.RootElement.GetProperty("serverUrl").GetString()!;
            }
            else
            {
                var parts = scannedData.Split('|');
                if (parts.Length < 3) return false;
                pairingId = Guid.Parse(parts[0]);
                code = parts[1];
                serverUrl = parts[2];
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse claim code");
            return false;
        }

        var baseUri = BuildBaseUri(serverUrl);
        var request = new PairRedeemRequest(pairingId, code);

        // We use a custom response record here since the shared contract doesn't include tokens yet
        // but the server now returns them.
        var response = await PostJsonAsync<PairRedeemRequest, RedeemResponseEx>(baseUri, "/v1/auth/pake/pair/redeem", request, cancellationToken);

        if (response != null && !string.IsNullOrEmpty(response.AccessToken))
        {
             await _tokenStore.SaveAsync(response.UserId, response.AccessToken, response.RefreshToken);
             _lastBaseUri = baseUri;
             SetAccessToken(response.AccessToken);

             // If vault is present, we should ideally import it, but we need the password.
             // For now, we just authenticate. The vault will remain locked until the user enters the password (if we implement that flow later).
             // Or maybe we can prompt for password *after* successful login.

             if (!string.IsNullOrEmpty(response.VaultDataBase64))
             {
                 // Store vault temporarily or just acknowledge we have it?
                 // Current MobileVaultService expects password to import.
             }

             await RefreshCurrentTenantAsync(cancellationToken);
             return true;
        }

        return false;
    }

    private class RedeemResponseEx
    {
        public string UserId { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public string? VaultDataBase64 { get; set; }
    }
}
