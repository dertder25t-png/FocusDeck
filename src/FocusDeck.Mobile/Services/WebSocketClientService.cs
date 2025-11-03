using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace FocusDeck.Mobile.Services;

/// <summary>
/// Service for managing WebSocket connections to the server
/// </summary>
public interface IWebSocketClientService
{
    bool IsConnected { get; }
    Task ConnectAsync(string serverUrl, string accessToken, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task SendAsync<T>(string messageType, T payload, CancellationToken cancellationToken = default);
    event EventHandler<WebSocketMessageEventArgs>? MessageReceived;
    event EventHandler<WebSocketStateChangedEventArgs>? StateChanged;
}

/// <summary>
/// WebSocket client implementation for real-time communication with FocusDeck server
/// </summary>
public class WebSocketClientService : IWebSocketClientService, IDisposable
{
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _receiveCts;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ILogger<WebSocketClientService> _logger;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public event EventHandler<WebSocketMessageEventArgs>? MessageReceived;
    public event EventHandler<WebSocketStateChangedEventArgs>? StateChanged;

    public WebSocketClientService(ILogger<WebSocketClientService> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(string serverUrl, string accessToken, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
        {
            _logger.LogWarning("WebSocket is already connected");
            return;
        }

        try
        {
            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            
            // Add authorization header
            _webSocket.Options.SetRequestHeader("Authorization", $"Bearer {accessToken}");

            // Convert http(s) URL to ws(s)
            var wsUrl = serverUrl.Replace("http://", "ws://").Replace("https://", "wss://");
            var uri = new Uri($"{wsUrl}/hubs/notifications");

            _logger.LogInformation("Connecting to WebSocket: {Url}", uri);
            await _webSocket.ConnectAsync(uri, cancellationToken);

            OnStateChanged(WebSocketState.Open);
            _logger.LogInformation("WebSocket connected successfully");

            // Start receiving messages
            _receiveCts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(_receiveCts.Token), _receiveCts.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to WebSocket");
            OnStateChanged(WebSocketState.Closed);
            throw;
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            _receiveCts?.Cancel();
            
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Client disconnecting",
                cancellationToken);

            OnStateChanged(WebSocketState.Closed);
            _logger.LogInformation("WebSocket disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while disconnecting WebSocket");
        }
    }

    public async Task SendAsync<T>(string messageType, T payload, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("WebSocket is not connected");
        }

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            var message = new
            {
                type = messageType,
                payload
            };

            var json = JsonSerializer.Serialize(message);
            var bytes = Encoding.UTF8.GetBytes(json);
            var segment = new ArraySegment<byte>(bytes);

            await _webSocket!.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken);
            _logger.LogDebug("Sent WebSocket message: {MessageType}", messageType);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (_webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var segment = new ArraySegment<byte>(buffer);
                var result = await _webSocket.ReceiveAsync(segment, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Server closed connection",
                        CancellationToken.None);
                    OnStateChanged(WebSocketState.Closed);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogDebug("Received WebSocket message: {Message}", message);
                    OnMessageReceived(message);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("WebSocket receive loop canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket receive loop");
            OnStateChanged(WebSocketState.Closed);
        }
    }

    private void OnMessageReceived(string message)
    {
        MessageReceived?.Invoke(this, new WebSocketMessageEventArgs(message));
    }

    private void OnStateChanged(WebSocketState state)
    {
        StateChanged?.Invoke(this, new WebSocketStateChangedEventArgs(state));
    }

    public void Dispose()
    {
        _receiveCts?.Cancel();
        _receiveCts?.Dispose();
        _webSocket?.Dispose();
        _sendLock.Dispose();
    }
}

public class WebSocketMessageEventArgs : EventArgs
{
    public string Message { get; }

    public WebSocketMessageEventArgs(string message)
    {
        Message = message;
    }
}

public class WebSocketStateChangedEventArgs : EventArgs
{
    public WebSocketState State { get; }

    public WebSocketStateChangedEventArgs(WebSocketState state)
    {
        State = state;
    }
}
