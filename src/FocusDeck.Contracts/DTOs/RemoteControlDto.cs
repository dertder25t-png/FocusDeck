using System;
using System.Collections.Generic;

namespace FocusDeck.Contracts.DTOs;

/// <summary>
/// DTO for registering a new device
/// </summary>
public class RegisterDeviceDto
{
    public string DeviceType { get; set; } = string.Empty; // "Desktop" or "Phone"
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Capabilities { get; set; } = new();
}

/// <summary>
/// Response after registering a device
/// </summary>
public class RegisterDeviceResponseDto
{
    public Guid DeviceId { get; set; }
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// DTO representing a device link
/// </summary>
public class DeviceLinkDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Capabilities { get; set; } = new();
    public DateTime LastSeenUtc { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a remote action
/// </summary>
public class CreateRemoteActionDto
{
    public string Kind { get; set; } = string.Empty; // "OpenNote", "OpenDeck", "RearrangeLayout", "StartFocus", "StopFocus"
    public Dictionary<string, object> Payload { get; set; } = new();
}

/// <summary>
/// DTO representing a remote action
/// </summary>
public class RemoteActionDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public Dictionary<string, object> Payload { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool? Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsPending { get; set; }
}

/// <summary>
/// DTO for completing a remote action
/// </summary>
public class CompleteRemoteActionDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for remote telemetry summary
/// </summary>
public class RemoteTelemetrySummaryDto
{
    public bool ActiveSession { get; set; }
    public int ProgressPercent { get; set; }
    public string? CurrentNoteId { get; set; }
    public string? FocusState { get; set; }
}
