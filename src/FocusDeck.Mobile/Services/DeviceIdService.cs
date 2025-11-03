namespace FocusDeck.Mobile.Services;

/// <summary>
/// Service for managing persistent device identification
/// </summary>
public interface IDeviceIdService
{
    /// <summary>
    /// Gets the unique device identifier, creating one if it doesn't exist
    /// </summary>
    string GetDeviceId();

    /// <summary>
    /// Regenerates the device identifier (useful for logout/reset scenarios)
    /// </summary>
    string RegenerateDeviceId();
}

/// <summary>
/// Implementation of device ID persistence using Preferences
/// </summary>
public class DeviceIdService : IDeviceIdService
{
    private const string DeviceIdKey = "device_id";
    private string? _cachedDeviceId;

    public string GetDeviceId()
    {
        if (_cachedDeviceId != null)
        {
            return _cachedDeviceId;
        }

        _cachedDeviceId = Preferences.Get(DeviceIdKey, string.Empty);

        if (string.IsNullOrEmpty(_cachedDeviceId))
        {
            _cachedDeviceId = GenerateNewDeviceId();
            Preferences.Set(DeviceIdKey, _cachedDeviceId);
        }

        return _cachedDeviceId;
    }

    public string RegenerateDeviceId()
    {
        _cachedDeviceId = GenerateNewDeviceId();
        Preferences.Set(DeviceIdKey, _cachedDeviceId);
        return _cachedDeviceId;
    }

    private static string GenerateNewDeviceId()
    {
        return Guid.NewGuid().ToString("N");
    }
}
