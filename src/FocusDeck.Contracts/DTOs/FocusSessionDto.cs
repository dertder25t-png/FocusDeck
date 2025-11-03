using System;
using System.Collections.Generic;

namespace FocusDeck.Contracts.DTOs;

/// <summary>
/// DTO for creating a new focus session
/// </summary>
public class CreateFocusSessionDto
{
    public FocusPolicyDto Policy { get; set; } = new();
}

/// <summary>
/// DTO for focus session policy
/// </summary>
public class FocusPolicyDto
{
    public bool Strict { get; set; } = false;
    public bool AutoBreak { get; set; } = true;
    public bool AutoDim { get; set; } = false;
    public bool NotifyPhone { get; set; } = false;
}

/// <summary>
/// DTO representing a focus session
/// </summary>
public class FocusSessionDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public FocusPolicyDto Policy { get; set; } = new();
    public int DistractionsCount { get; set; }
    public DateTime? LastRecoverySuggestionAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for submitting a focus signal
/// </summary>
public class SubmitSignalDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty; // "PhoneMotion", "PhoneScreen", "Keyboard", "Mouse", "AmbientNoise"
    public double Value { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// DTO for focus distraction event
/// </summary>
public class FocusDistractionDto
{
    public string Reason { get; set; } = string.Empty;
    public DateTime At { get; set; }
}

/// <summary>
/// DTO for focus recovery suggestion event
/// </summary>
public class FocusRecoverySuggestionDto
{
    public string Suggestion { get; set; } = string.Empty; // "Take 2-min break" or "Enable Lock Mode"
}
