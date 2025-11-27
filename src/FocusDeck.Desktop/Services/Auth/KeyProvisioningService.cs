using System;
using System.Security.Cryptography;
using System.Text;
using FocusDeck.Services.Implementations.Core;
using FocusDeck.Shared.Contracts.Auth;
using FocusDeck.Contracts.MultiTenancy;
using FocusDeck.Shared.Security;
using FocusDeck.Desktop.Services;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Desktop.Services.Auth;

public interface IKeyProvisioningService
{
    Task<bool> RegisterAsync(string userId, string password, CancellationToken ct = default);
    Task<(string accessToken, string refreshToken)?> LoginAsync(string userId, string password, CancellationToken ct = default);

    Task<(Guid pairingId, string code)> PairStartAsync(CancellationToken ct = default);
    Task<bool> PairTransferAsync(Guid pairingId, string password, CancellationToken ct = default);
    Task<bool> PairRedeemAsync(Guid pairingId, string code, string password, CancellationToken ct = default);

    void StartAutoRefresh();
    void StopAutoRefresh();

    Task LogoutAsync(CancellationToken ct = default);
    event EventHandler<ForceLogoutEventArgs>? ForcedLogout;

    CurrentTenantDto? CurrentTenant { get; }
    Task<CurrentTenantDto?> RefreshCurrentTenantAsync(CancellationToken ct = default);
    event EventHandler<CurrentTenantDto?>? CurrentTenantChanged;
}

public class KeyProvisioningService : IKeyProvisioningService
{
    private readonly IApiClient _api;
    private readonly EncryptionService _encryption;
    private readonly IRemoteControllerService _remote;
    private readonly ILogger<KeyProvisioningService> _logger;

    private readonly TokenStore _tokenStore;
    private Timer? _refreshTimer;
    private bool _notificationsConnected;
    private CurrentTenantDto? _currentTenant;

    public event EventHandler<ForceLogoutEventArgs>? ForcedLogout;
    public event EventHandler<CurrentTenantDto?>? CurrentTenantChanged;

    public CurrentTenantDto? CurrentTenant
    {
        get => _currentTenant;
        private set
        {
            if (_currentTenant?.Id == value?.Id && _currentTenant?.Name == value?.Name) return;
            _currentTenant = value;
            CurrentTenantChanged?.Invoke(this, value);
        }
    }

    public KeyProvisioningService(
        IApiClient apiClient,
        EncryptionService encryption,
        IRemoteControllerService remoteController,
        ILogger<KeyProvisioningService> logger)
    {
        _api = apiClient;
        _encryption = encryption;
        _remote = remoteController;
        _logger = logger;
        _tokenStore = new TokenStore();
        _remote.ForcedLogout += OnRemoteForcedLogout;
    }

    public async Task<bool> RegisterAsync(string userId, string password, CancellationToken ct = default)
    {
        var start = await _api.PostAsync<RegisterStartResponse>("/v1/auth/pake/register/start", new RegisterStartRequest(userId), ct);
        if (start == null) return false;

        if (!string.Equals(start.Algorithm, Srp.Algorithm, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unexpected SRP algorithm: {start.Algorithm}");
        }

        if (!string.Equals(start.ModulusHex, Srp.ModulusHex, StringComparison.OrdinalIgnoreCase) || start.Generator != (int)Srp.G)
        {
            throw new InvalidOperationException("Unsupported SRP parameters from server");
        }

        var salt = Convert.FromBase64String(start.SaltBase64);
        var privateKey = Srp.ComputePrivateKey(salt, userId, password);
        var verifier = Srp.ComputeVerifier(privateKey);
        var verifierB64 = Convert.ToBase64String(Srp.ToBigEndian(verifier));

        if (!_encryption.KeyExists)
        {
            _encryption.GenerateKeyPair();
        }

        var vaultExport = _encryption.ExportKeyEncryptedDetailed(password);
        var vault = vaultExport.CipherText;

        var finish = await _api.PostAsync<RegisterFinishResponse>("/v1/auth/pake/register/finish",
            new RegisterFinishRequest(userId, start.SaltBase64, verifierB64, vault, vaultExport.KdfMetadataJson, vaultExport.CipherSuite), ct);

        return finish?.Success == true;
    }

    public async Task<(string accessToken, string refreshToken)?> LoginAsync(string userId, string password, CancellationToken ct = default)
    {
        var deviceId = ComputeDeviceId();
        var deviceName = Environment.MachineName;
        const string devicePlatform = "windows";

        var (clientSecret, clientPublic) = Srp.GenerateClientEphemeral();
        var clientPublicB64 = Convert.ToBase64String(Srp.ToBigEndian(clientPublic));

        LoginStartResponse? start;
        try
        {
            start = await _api.PostAsync<LoginStartResponse>("/v1/auth/pake/login/start",
                new LoginStartRequest(userId, clientPublicB64, deviceId, deviceName, devicePlatform), ct);
        }
        catch
        {
            return null;
        }

        if (start == null)
        {
            return null;
        }

        if (!string.Equals(start.Algorithm, Srp.Algorithm, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unexpected SRP algorithm: {start.Algorithm}");
        }

        if (!string.Equals(start.ModulusHex, Srp.ModulusHex, StringComparison.OrdinalIgnoreCase) || start.Generator != (int)Srp.G)
        {
            throw new InvalidOperationException("Unsupported SRP parameters from server");
        }

        var salt = Convert.FromBase64String(start.SaltBase64);
        var serverPublic = Srp.FromBigEndian(Convert.FromBase64String(start.ServerPublicEphemeralBase64));
        var privateKey = Srp.ComputePrivateKey(salt, userId, password);
        var scramble = Srp.ComputeScramble(clientPublic, serverPublic);
        if (scramble.Sign == 0)
        {
            throw new CryptographicException("SRP scramble parameter was zero");
        }

        var sessionSecret = Srp.ComputeClientSession(serverPublic, privateKey, clientSecret, scramble);
        var sessionKey = Srp.ComputeSessionKey(sessionSecret);
        var clientProofBytes = Srp.ComputeClientProof(clientPublic, serverPublic, sessionKey);
        var clientProof = Convert.ToBase64String(clientProofBytes);

        var finish = await _api.PostAsync<LoginFinishResponse>("/v1/auth/pake/login/finish",
            new LoginFinishRequest(userId, start.SessionId, clientProof, deviceId, deviceName, devicePlatform), ct);

        if (finish == null || !finish.Success)
        {
            return null;
        }

        var expectedServerProof = Srp.ComputeServerProof(clientPublic, clientProofBytes, sessionKey);
        var actualServerProof = Convert.FromBase64String(finish.ServerProofBase64);
        if (!CryptographicOperations.FixedTimeEquals(expectedServerProof, actualServerProof))
        {
            throw new CryptographicException("Server proof verification failed");
        }

        if (!_encryption.KeyExists && finish.HasVault)
        {
            // Client can request vault via pairing redeem; skip auto-fetch to keep flow explicit.
        }

        string accessToken = finish.AccessToken;
        string refreshToken = finish.RefreshToken;
        _api.AccessToken = accessToken;
        _tokenStore.Save(accessToken, refreshToken, userId);
        StartAutoRefresh();
        if (finish.ExpiresIn > 0)
        {
            var due = TimeSpan.FromSeconds(Math.Max(60, (int)(finish.ExpiresIn * 0.8)));
            _refreshTimer?.Change(due, due);
        }

        await EnsureNotificationsConnectedAsync(userId, ct);
        await RefreshCurrentTenantAsync(ct);

        return (accessToken, refreshToken);
    }

    public async Task<(Guid pairingId, string code)> PairStartAsync(CancellationToken ct = default)
    {
        var res = await _api.PostAsync<dynamic>("/v1/auth/pake/pair/start", new { sourceDeviceId = Environment.MachineName }, ct);
        if (res == null) throw new InvalidOperationException("Pair start failed");
        return ((Guid)res.pairingId, (string)res.code);
    }

    public async Task<bool> PairTransferAsync(Guid pairingId, string password, CancellationToken ct = default)
    {
        if (!_encryption.KeyExists) return false;
        var vaultExport = _encryption.ExportKeyEncryptedDetailed(password);
        var res = await _api.PostAsync<dynamic>(
            "/v1/auth/pake/pair/transfer",
            new PairTransferRequest(pairingId, vaultExport.CipherText, vaultExport.KdfMetadataJson, vaultExport.CipherSuite, null),
            ct);
        return res != null;
    }

    public async Task<bool> PairRedeemAsync(Guid pairingId, string code, string password, CancellationToken ct = default)
    {
        var res = await _api.PostAsync<dynamic>("/v1/auth/pake/pair/redeem", new { pairingId, code }, ct);
        if (res == null) return false;
        string vault = res.vaultDataBase64;
        return _encryption.ImportKeyEncrypted(vault, password);
    }

    public void StartAutoRefresh()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = new Timer(async _ =>
        {
            try { await RefreshAsync(); } catch { }
        }, null, TimeSpan.FromMinutes(50), TimeSpan.FromMinutes(50));
    }

    public void StopAutoRefresh()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
    }

    public async Task<CurrentTenantDto?> RefreshCurrentTenantAsync(CancellationToken ct = default)
    {
        try
        {
            var tenant = await _api.GetAsync<CurrentTenantDto>("/v1/tenants/current", ct);
            CurrentTenant = tenant;
            return tenant;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh current tenant");
            CurrentTenant = null;
            return null;
        }
    }

    private async Task RefreshAsync()
    {
        var rec = _tokenStore.Load();
        if (rec == null || string.IsNullOrEmpty(rec.RefreshToken)) return;
        var res = await _api.PostAsync<dynamic>("/v1/auth/refresh", new { accessToken = (string?)null, refreshToken = rec.RefreshToken, clientId = Environment.MachineName });
        if (res != null)
        {
            string newAccess = res.accessToken;
            string? newRefresh = res.refreshToken;
            _api.AccessToken = newAccess;
            _tokenStore.Save(newAccess, newRefresh ?? rec.RefreshToken, rec.UserId);
            if (res.expiresIn is int seconds && seconds > 0)
            {
                var due = TimeSpan.FromSeconds(Math.Max(60, (int)(seconds * 0.8)));
                _refreshTimer?.Change(due, due);
            }
        }
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        try { await _api.PostAsync<dynamic>("/v1/auth/logout", new { }, ct); } catch { }
        await PerformLocalLogoutAsync();
    }

    private static string ComputeDeviceId()
    {
        var raw = $"{Environment.MachineName}|{Environment.UserName}|FocusDeck";
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task EnsureNotificationsConnectedAsync(string userId, CancellationToken ct)
    {
        if (_notificationsConnected)
        {
            return;
        }

        var baseAddress = _api.BaseAddress;
        if (baseAddress == null)
        {
            _logger.LogWarning("Cannot connect to notifications hub because API base address is not set.");
            return;
        }

        try
        {
            var hubUri = new Uri(baseAddress, "/hubs/notifications");
            await _remote.ConnectAsync(hubUri.ToString(), userId, ct);
            _notificationsConnected = true;
            _logger.LogInformation("Connected to notifications hub for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to notifications hub");
        }
    }

    private async Task PerformLocalLogoutAsync()
    {
        _api.AccessToken = null;
        _tokenStore.Clear();
        StopAutoRefresh();

        if (_notificationsConnected)
        {
            try
            {
                await _remote.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to disconnect notifications hub cleanly");
            }
        }

        _notificationsConnected = false;
    }

    private async void OnRemoteForcedLogout(object? sender, ForceLogoutEventArgs e)
    {
        try
        {
            await PerformLocalLogoutAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle forced logout");
        }

        ForcedLogout?.Invoke(this, e);
    }
}
