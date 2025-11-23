using FocusDeck.Mobile.Services;
using Microsoft.Maui.ApplicationModel;

namespace FocusDeck.Mobile.Services;

public class MobileActionHandler
{
    private readonly ISignalRService _signalRService;
    private readonly IMobileNotificationService _notificationService;
    private readonly ILogger<MobileActionHandler> _logger;

    public MobileActionHandler(
        ISignalRService signalRService,
        IMobileNotificationService notificationService,
        ILogger<MobileActionHandler> logger)
    {
        _signalRService = signalRService;
        _notificationService = notificationService;
        _logger = logger;

        _signalRService.ActionReceived += OnActionReceived;
        _signalRService.JarvisRunUpdated += OnJarvisRunUpdated;
    }

    private void OnJarvisRunUpdated(object? sender, FocusDeck.Shared.SignalR.Notifications.JarvisRunUpdate e)
    {
        if (e.Status == "completed")
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await _notificationService.SendLocalNotificationAsync("Jarvis", $"Workflow completed: {e.Summary ?? "No summary"}", 0);
            });
        }
    }

    private void OnActionReceived(object? sender, RemoteActionEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                switch (e.Kind)
                {
                    case "ShowToast":
                        var message = e.Payload?.ToString() ?? "Notification";
                        await _notificationService.SendLocalNotificationAsync("FocusDeck", message, 0);
                        break;

                    case "OpenUrl":
                        if (e.Payload is string url && Uri.TryCreate(url, UriKind.Absolute, out var uri))
                        {
                            await Launcher.OpenAsync(uri);
                        }
                        break;

                    case "OpenNote":
                        // Stub: just notify for now
                        await _notificationService.SendLocalNotificationAsync("Open Note", $"Requested note: {e.Payload}", 0);
                        break;

                    default:
                        _logger.LogWarning("Unknown mobile action: {Kind}", e.Kind);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle mobile action {Kind}", e.Kind);
            }
        });
    }
}
