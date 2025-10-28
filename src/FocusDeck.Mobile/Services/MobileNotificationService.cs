using System.Diagnostics;

namespace FocusDeck.Mobile.Services;

/// <summary>
/// Stub implementation of mobile notification service.
/// Platform-specific implementations for iOS and Android notifications.
/// </summary>
public class MobileNotificationService : IMobileNotificationService
{
    public Task<bool> RequestPermissionAsync()
    {
        // TODO: Platform-specific permission request
        // iOS: UNUserNotificationCenter
        // Android: ActivityCompat.RequestPermissions
        return Task.FromResult(true);
    }

    public Task SendLocalNotificationAsync(string title, string message, int delaySeconds = 10)
    {
        try
        {
            // TODO: Platform-specific local notification scheduling
            // iOS: UNUserNotificationCenter.Current.AddNotificationRequest
            // Android: NotificationManagerCompat.Notify
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current?.MainPage?.DisplayAlert(title, message, "OK");
            });
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to send notification: {ex.Message}");
            return Task.CompletedTask;
        }
    }

    public Task SendStudySessionReminderAsync(string sessionName, DateTime startTime)
    {
        var message = $"Study session '{sessionName}' starts at {startTime:t}";
        var delaySeconds = (int)(startTime - DateTime.Now).TotalSeconds;
        
        if (delaySeconds > 0)
        {
            return SendLocalNotificationAsync("Study Reminder", message, delaySeconds);
        }
        
        return Task.CompletedTask;
    }

    public Task CancelNotificationAsync(int notificationId)
    {
        // TODO: Platform-specific notification cancellation
        // iOS: UNUserNotificationCenter.Current.RemoveDeliveredNotifications
        // Android: NotificationManagerCompat.Cancel
        
        return Task.CompletedTask;
    }
}
