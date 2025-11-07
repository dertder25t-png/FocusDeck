using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FocusDeck.Mobile.Services.Auth;
using FocusDeck.Shared.Contracts.Auth;
using Microsoft.Maui.Storage;

namespace FocusDeck.Mobile.Services;

public interface IDevicePairingService
{
    bool IsPaired { get; }
    string? PairedDeviceId { get; }
    string? PairingCode { get; }

    Task<PairingResult> InitiatePairingAsync(CancellationToken cancellationToken = default);
    Task<PairingResult> CompletePairingAsync(string pairingCode, string password, CancellationToken cancellationToken = default);
    Task<bool> UnpairAsync(CancellationToken cancellationToken = default);
    Task<bool> VerifyPairingAsync(CancellationToken cancellationToken = default);
}

public class DevicePairingService : IDevicePairingService
{
    private readonly ILogger<DevicePairingService> _logger;
    private readonly IDeviceIdService _deviceIdService;
    private readonly MobileTokenStore _tokenStore;
    private readonly MobileVaultService _vaultService;
    private readonly HttpClient _httpClient;

    private Guid? _pairingId;
    private string? _pairedDeviceId;
    private string? _pairingCode;

    public bool IsPaired => !string.IsNullOrEmpty(_pairedDeviceId);
    public string? PairedDeviceId => _pairedDeviceId;
    public string? PairingCode => _pairingCode;

    public DevicePairingService(
        ILogger<DevicePairingService> logger,
        IDeviceIdService deviceIdService,
        MobileTokenStore tokenStore,
        MobileVaultService vaultService)
    {
        _logger = logger;
        _deviceIdService = deviceIdService;
        _tokenStore = tokenStore;
        _vaultService = vaultService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public async Task<PairingResult> InitiatePairingAsync(CancellationToken cancellationToken = default)
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new PairingResult { Success = false, Message = "Sign in before pairing." };
        }

        var endpoint = BuildUri("/v1/auth/pake/pair/start");
        if (endpoint == null)
        {
            return new PairingResult { Success = false, Message = "Cloud server URL not configured." };
        }

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(new PairStartRequest(await _deviceIdService.GetDeviceIdAsync()))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Pair start failed with status {Status}", response.StatusCode);
            return new PairingResult { Success = false, Message = "Failed to initiate pairing." };
        }

        var payload = await response.Content.ReadFromJsonAsync<PairStartResponse>(cancellationToken: cancellationToken);
        if (payload == null)
        {
            return new PairingResult { Success = false, Message = "Invalid server response." };
        }

        _pairingId = payload.PairingId;
        _pairingCode = payload.Code;

        return new PairingResult
        {
            Success = true,
            PairingCode = payload.Code,
            Message = "Enter this code on your desktop to complete pairing."
        };
    }

    public async Task<PairingResult> CompletePairingAsync(string pairingCode, string password, CancellationToken cancellationToken = default)
    {
        if (_pairingId == null)
        {
            return new PairingResult { Success = false, Message = "Start pairing first." };
        }

        var token = await _tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new PairingResult { Success = false, Message = "Sign in before pairing." };
        }

        var endpoint = BuildUri("/v1/auth/pake/pair/transfer");
        if (endpoint == null)
        {
            return new PairingResult { Success = false, Message = "Cloud server URL not configured." };
        }

        var vault = await _vaultService.ExportEncryptedAsync(password);
        var payload = new PairTransferRequest(
            _pairingId.Value,
            vault.CipherText,
            vault.KdfMetadataJson,
            vault.CipherSuite,
            await _deviceIdService.GetDeviceIdAsync());

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Pair transfer failed with status {Status}", response.StatusCode);
            return new PairingResult { Success = false, Message = "Pair transfer failed." };
        }

        _pairedDeviceId = await _deviceIdService.GetDeviceIdAsync();
        _pairingCode = null;
        _pairingId = null;

        return new PairingResult
        {
            Success = true,
            DeviceId = _pairedDeviceId,
            Message = "Device paired successfully."
        };
    }

    public Task<bool> UnpairAsync(CancellationToken cancellationToken = default)
    {
        _pairedDeviceId = null;
        _pairingCode = null;
        _pairingId = null;
        return Task.FromResult(true);
    }

    public Task<bool> VerifyPairingAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(IsPaired);
    }

    private Uri? BuildUri(string path)
    {
        var url = Preferences.Get("cloud_server_url", string.Empty);
        if (string.IsNullOrWhiteSpace(url)) return null;
        var baseUri = new Uri(url.TrimEnd('/') + "/");
        return new Uri(baseUri, path.TrimStart('/'));
    }

    private record PairStartResponse(Guid PairingId, string Code);
}

public class PairingResult
{
    public bool Success { get; set; }
    public string? PairingCode { get; set; }
    public string? DeviceId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

