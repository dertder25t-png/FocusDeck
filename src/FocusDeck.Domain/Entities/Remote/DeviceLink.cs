using System;
using System.Collections.Generic;
using System.Text.Json;

namespace FocusDeck.Domain.Entities.Remote;

/// <summary>
/// Represents a device (Desktop or Phone) registered for remote control capabilities
/// </summary>
public class DeviceLink : IMustHaveTenant
{
    /// <summary>
    /// Unique identifier for the device
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User ID this device belongs to
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Type of device (Desktop or Phone)
    /// </summary>
    public DeviceType DeviceType { get; set; }

    /// <summary>
    /// User-friendly name for the device
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// JSON blob storing device capabilities
    /// </summary>
    public string CapabilitiesJson { get; set; } = "{}";

    /// <summary>
    /// Last time the device was seen/active
    /// </summary>
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the device was registered
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Device capabilities deserialized from JSON
    /// </summary>
    public Dictionary<string, object> GetCapabilities()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(CapabilitiesJson) 
                   ?? new Dictionary<string, object>();
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Set device capabilities as JSON
    /// </summary>
    public void SetCapabilities(Dictionary<string, object> capabilities)
    {
        CapabilitiesJson = JsonSerializer.Serialize(capabilities);
    }

    public Guid TenantId { get; set; }
}

/// <summary>
/// Device type enumeration
/// </summary>
public enum DeviceType
{
    Desktop = 0,
    Phone = 1
}
