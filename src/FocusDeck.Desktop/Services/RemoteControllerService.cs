using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Desktop.Services;

/// <summary>
/// Service for handling remote control commands from mobile devices
/// </summary>
public interface IRemoteControllerService
{
    Task ConnectAsync(string hubUrl, string userId, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task StartTelemetryAsync(CancellationToken cancellationToken = default);
    Task StopTelemetryAsync();
    event EventHandler<RemoteActionEventArgs>? ActionReceived;
}

/// <summary>
/// Event args for remote actions
/// </summary>
public class RemoteActionEventArgs : EventArgs
{
    public string ActionId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public object? Payload { get; set; }
}

public class RemoteControllerService : IRemoteControllerService, IDisposable
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<RemoteControllerService> _logger;
    private HubConnection? _hubConnection;
    private Timer? _telemetryTimer;
    private bool _isConnected;

    public event EventHandler<RemoteActionEventArgs>? ActionReceived;

    public RemoteControllerService(IApiClient apiClient, ILogger<RemoteControllerService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Connect to SignalR hub and subscribe to remote actions
    /// </summary>
    public async Task ConnectAsync(string hubUrl, string userId, CancellationToken cancellationToken = default)
    {
        if (_isConnected)
        {
            _logger.LogWarning("Already connected to SignalR hub");
            return;
        }

        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // Subscribe to remote action events
            _hubConnection.On<string, string, object>("RemoteActionCreated", (actionId, kind, payload) =>
            {
                _logger.LogInformation("Received remote action: {ActionId} ({Kind})", actionId, kind);
                ActionReceived?.Invoke(this, new RemoteActionEventArgs
                {
                    ActionId = actionId,
                    Kind = kind,
                    Payload = payload
                });
                
                // Handle the action
                HandleActionAsync(actionId, kind, payload).ConfigureAwait(false);
            });

            await _hubConnection.StartAsync(cancellationToken);
            await _hubConnection.InvokeAsync("JoinUserGroup", userId, cancellationToken);

            _isConnected = true;
            _logger.LogInformation("Connected to SignalR hub and joined user group: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SignalR hub");
            throw;
        }
    }

    /// <summary>
    /// Disconnect from SignalR hub
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }

        _isConnected = false;
        _logger.LogInformation("Disconnected from SignalR hub");
    }

    /// <summary>
    /// Start publishing telemetry every 2 seconds
    /// </summary>
    public Task StartTelemetryAsync(CancellationToken cancellationToken = default)
    {
        if (_telemetryTimer != null)
        {
            _logger.LogWarning("Telemetry already running");
            return Task.CompletedTask;
        }

        _telemetryTimer = new Timer(async _ =>
        {
            await PublishTelemetryAsync();
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

        _logger.LogInformation("Started telemetry publishing");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop publishing telemetry
    /// </summary>
    public Task StopTelemetryAsync()
    {
        if (_telemetryTimer != null)
        {
            _telemetryTimer.Dispose();
            _telemetryTimer = null;
        }

        _logger.LogInformation("Stopped telemetry publishing");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handle a remote action
    /// </summary>
    private async Task HandleActionAsync(string actionId, string kind, object payload)
    {
        try
        {
            bool success = false;
            string? errorMessage = null;

            switch (kind)
            {
                case "OpenNote":
                    success = await HandleOpenNoteAsync(payload);
                    break;
                
                case "OpenDeck":
                    success = await HandleOpenDeckAsync(payload);
                    break;
                
                case "RearrangeLayout":
                    success = await HandleRearrangeLayoutAsync(payload);
                    break;
                
                case "StartFocus":
                    success = await HandleStartFocusAsync(payload);
                    break;
                
                case "StopFocus":
                    success = await HandleStopFocusAsync(payload);
                    break;
                
                default:
                    errorMessage = $"Unknown action kind: {kind}";
                    break;
            }

            // Mark action as complete
            await CompleteActionAsync(actionId, success, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle action: {ActionId}", actionId);
            await CompleteActionAsync(actionId, false, ex.Message);
        }
    }

    /// <summary>
    /// Handle OpenNote action
    /// </summary>
    private Task<bool> HandleOpenNoteAsync(object payload)
    {
        // TODO: Implement navigation to note
        // This would integrate with the existing navigation service
        _logger.LogInformation("OpenNote action - payload: {Payload}", payload);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Handle OpenDeck action (stub)
    /// </summary>
    private Task<bool> HandleOpenDeckAsync(object payload)
    {
        // TODO: Implement navigation to deck (stub for now)
        _logger.LogInformation("OpenDeck action - payload: {Payload}", payload);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Handle RearrangeLayout action
    /// </summary>
    private Task<bool> HandleRearrangeLayoutAsync(object payload)
    {
        // TODO: Implement layout rearrangement
        // This would integrate with existing layout management
        _logger.LogInformation("RearrangeLayout action - payload: {Payload}", payload);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Handle StartFocus action
    /// </summary>
    private Task<bool> HandleStartFocusAsync(object payload)
    {
        // TODO: Call existing focus start endpoint
        _logger.LogInformation("StartFocus action - payload: {Payload}", payload);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Handle StopFocus action
    /// </summary>
    private Task<bool> HandleStopFocusAsync(object payload)
    {
        // TODO: Call existing focus stop endpoint
        _logger.LogInformation("StopFocus action - payload: {Payload}", payload);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Complete a remote action via API
    /// </summary>
    private async Task CompleteActionAsync(string actionId, bool success, string? errorMessage)
    {
        try
        {
            await _apiClient.PostAsync<object>($"/v1/remote/actions/{actionId}/complete", new
            {
                success = success,
                errorMessage = errorMessage
            });
            
            _logger.LogInformation("Marked action as complete: {ActionId} (Success={Success})", actionId, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete action: {ActionId}", actionId);
        }
    }

    /// <summary>
    /// Publish telemetry to server
    /// </summary>
    private async Task PublishTelemetryAsync()
    {
        if (_hubConnection == null || !_isConnected)
        {
            return;
        }

        try
        {
            // Get current telemetry data
            // TODO: Integrate with actual app state
            var progressPercent = 0; // Would get from active session
            var focusState = "idle"; // Would get from focus state
            string? activeNoteId = null; // Would get from current view

            // Send via SignalR - use the correct hub method name
            await _hubConnection.InvokeAsync("SendTelemetry", "test-user", progressPercent, focusState, activeNoteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish telemetry");
        }
    }

    public void Dispose()
    {
        _telemetryTimer?.Dispose();
        _hubConnection?.DisposeAsync().AsTask().Wait();
    }
}
