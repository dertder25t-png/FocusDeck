namespace FocusDeck.Domain.Entities.Auth;

public class AuthEventLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty; // e.g., PAKE_LOGIN_SUCCESS
    public string? UserId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsSuccess { get; set; }
    public string? FailureReason { get; set; }
    public string? RemoteIp { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? UserAgent { get; set; }
    public string? MetadataJson { get; set; }
}

