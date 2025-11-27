namespace FocusDeck.Shared.SignalR.Notifications;

public interface INotificationClientContract
{
    Task RemoteActionCreated(string ActionId, string Kind, object Payload);
    Task RemoteTelemetry(TelemetryUpdate payload);
    Task ForceLogout(ForceLogoutMessage payload);

    /// <summary>
    /// Notifies a client when a Jarvis workflow run changes status.
    /// </summary>
    Task JarvisRunUpdated(JarvisRunUpdate payload);
}

public sealed record TelemetryUpdate(int ProgressPercent, string FocusState, string? ActiveNoteId);

public sealed record ForceLogoutMessage(string Reason, string? DeviceId);

public sealed record JarvisRunUpdate(Guid RunId, string WorkflowId, string Status, string? Summary, DateTime UpdatedAtUtc);
