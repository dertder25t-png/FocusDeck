namespace FocusDeck.Mobile.Services;

/// <summary>
/// Mobile notification service for local and push notifications.
/// Handles permissions, scheduling, and cancellation of notifications.
/// </summary>
public interface IMobileNotificationService
{
    Task<bool> RequestPermissionAsync();
    Task SendLocalNotificationAsync(string title, string message, int delaySeconds = 10);
    Task SendStudySessionReminderAsync(string sessionName, DateTime startTime);
    Task CancelNotificationAsync(int notificationId);
}
