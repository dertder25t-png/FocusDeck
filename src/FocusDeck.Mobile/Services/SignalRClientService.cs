using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using FocusDeck.Shared.SignalR.Notifications;
using CommunityToolkit.Mvvm.Messaging;
using FocusDeck.Mobile.Messages;

namespace FocusDeck.Mobile.Services;

public interface ISignalRService
{
    bool IsConnected { get; }
    Task ConnectAsync(string serverUrl, string accessToken, string userId, CancellationToken cancellationToken = default);
    Task DisconnectAsync();

    event EventHandler<JarvisRunUpdate>? JarvisRunUpdated;
    event EventHandler<RemoteActionEventArgs>? ActionReceived;
}

public class RemoteActionEventArgs : EventArgs
{
    public string ActionId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public object? Payload { get; set; }
}

public class SignalRClientService : ISignalRService, IAsyncDisposable
{
    private readonly ILogger<SignalRClientService> _logger;
    private readonly IMessenger _messenger;
    private HubConnection? _hubConnection;
    private bool _isConnected;

    public event EventHandler<JarvisRunUpdate>? JarvisRunUpdated;
    public event EventHandler<RemoteActionEventArgs>? ActionReceived;

    public bool IsConnected => _isConnected && _hubConnection?.State == HubConnectionState.Connected;

    public SignalRClientService(ILogger<SignalRClientService> logger, IMessenger messenger)
    {
        _logger = logger;
        _messenger = messenger;
    }

    public async Task ConnectAsync(string serverUrl, string accessToken, string userId, CancellationToken cancellationToken = default)
    {
        if (IsConnected) return;

        try
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }

            // Convert http(s) URL to base if needed, though HubConnectionBuilder handles it
            var hubUrl = new Uri(new Uri(serverUrl), "/hubs/notifications");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                })
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<string, string, object>(nameof(INotificationClientContract.RemoteActionCreated), (actionId, kind, payload) =>
            {
                _logger.LogInformation("Received remote action: {Kind}", kind);
                ActionReceived?.Invoke(this, new RemoteActionEventArgs { ActionId = actionId, Kind = kind, Payload = payload });
                // We might want to dispatch a messenger event here too
            });

            _hubConnection.On<JarvisRunUpdate>(nameof(INotificationClientContract.JarvisRunUpdated), update =>
            {
                _logger.LogInformation("Jarvis run updated: {Status}", update.Status);
                JarvisRunUpdated?.Invoke(this, update);
            });

            _hubConnection.On<ForceLogoutMessage>(nameof(INotificationClientContract.ForceLogout), msg =>
            {
                 _messenger.Send(new ForcedLogoutMessage(msg.Reason));
            });

            await _hubConnection.StartAsync(cancellationToken);

            // Join user group if required by server logic
            try
            {
                await _hubConnection.InvokeAsync("JoinUserGroup", userId, cancellationToken);
            }
            catch (Exception ex)
            {
                 _logger.LogWarning(ex, "Failed to join user group");
            }

            _isConnected = true;
            _logger.LogInformation("SignalR connected.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect SignalR");
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
        _isConnected = false;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
