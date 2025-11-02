using Microsoft.AspNetCore.SignalR;

namespace FocusDeck.Server.Hubs;

/// <summary>
/// SignalR hub for real-time notifications to connected clients
/// </summary>
public class NotificationsHub : Hub<INotificationClient>
{
    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(ILogger<NotificationsHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a user-specific group for targeted notifications
    /// </summary>
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        _logger.LogInformation("Client {ConnectionId} joined user group: {UserId}", Context.ConnectionId, userId);
    }

    /// <summary>
    /// Leave a user-specific group
    /// </summary>
    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        _logger.LogInformation("Client {ConnectionId} left user group: {UserId}", Context.ConnectionId, userId);
    }

    /// <summary>
    /// Join a session-specific group for session updates
    /// </summary>
    public async Task JoinSessionGroup(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session:{sessionId}");
        _logger.LogInformation("Client {ConnectionId} joined session group: {SessionId}", Context.ConnectionId, sessionId);
    }

    /// <summary>
    /// Leave a session-specific group
    /// </summary>
    public async Task LeaveSessionGroup(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session:{sessionId}");
        _logger.LogInformation("Client {ConnectionId} left session group: {SessionId}", Context.ConnectionId, sessionId);
    }
}

/// <summary>
/// Typed client interface for strongly-typed notifications
/// </summary>
public interface INotificationClient
{
    /// <summary>
    /// Notify client of a new study session
    /// </summary>
    Task SessionCreated(string sessionId, string message);

    /// <summary>
    /// Notify client of session updates
    /// </summary>
    Task SessionUpdated(string sessionId, string status, string message);

    /// <summary>
    /// Notify client of session completion
    /// </summary>
    Task SessionCompleted(string sessionId, int durationMinutes, string message);

    /// <summary>
    /// Notify client of automation execution
    /// </summary>
    Task AutomationExecuted(string automationId, bool success, string message);

    /// <summary>
    /// Notify client of job completion (transcription, summarization, etc.)
    /// </summary>
    Task JobCompleted(string jobId, string jobType, bool success, string message, object? result);

    /// <summary>
    /// Notify client of job progress updates
    /// </summary>
    Task JobProgress(string jobId, string jobType, int progressPercent, string message);

    /// <summary>
    /// General notification message
    /// </summary>
    Task NotificationReceived(string title, string message, string severity);
}
