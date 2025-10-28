namespace FocusDeck.Services.Implementations.Core;

using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using FocusDeck.Services.Abstractions;

/// <summary>
/// Manages device registration and multi-device coordination for cloud sync
/// </summary>
public class DeviceRegistryService : IDeviceRegistryService
{
    private readonly string _deviceId;
    private readonly string _deviceName;
    private const string DEVICES_REGISTRY_FILE = "devices_registry.json";

    public string DeviceId => _deviceId;
    public string DeviceName => _deviceName;

    public DeviceRegistryService()
    {
        _deviceId = GenerateDeviceId();
        _deviceName = GenerateDeviceName();
    }

    /// <summary>
    /// Register this device for cloud sync
    /// </summary>
    public async Task<bool> RegisterDeviceAsync(ICloudProvider provider)
    {
        try
        {
            var deviceInfo = new DeviceInfo
            {
                DeviceId = _deviceId,
                DeviceName = _deviceName,
                Platform = GetPlatform(),
                RegisteredTime = DateTime.Now,
                LastSyncTime = DateTime.Now,
                IsOnline = true
            };

            // Store device info locally and in cloud
            var registryPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FocusDeck", "Sync", DEVICES_REGISTRY_FILE
            );

            var directory = Path.GetDirectoryName(registryPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create registry entry
            var json = System.Text.Json.JsonSerializer.Serialize(new[] { deviceInfo }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(registryPath, json);

            // Upload to cloud
            await provider.UploadFileAsync(
                registryPath,
                $"/FocusDeck/sync_metadata/device_{_deviceId}.json"
            );

            System.Diagnostics.Debug.WriteLine($"Device registered: {_deviceName} ({_deviceId})");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to register device: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get list of all devices synced to this account
    /// </summary>
    public async Task<DeviceInfo[]> GetRegisteredDevicesAsync(ICloudProvider provider)
    {
        try
        {
            var devices = new List<DeviceInfo>();
            var deviceFiles = await provider.ListFilesAsync("/FocusDeck/sync_metadata/");

            foreach (var file in deviceFiles.Where(f => f.Name.StartsWith("device_") && f.Name.EndsWith(".json")))
            {
                try
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), file.Name);
                    await provider.DownloadFileAsync(file.Path, tempPath);

                    var json = File.ReadAllText(tempPath);
                    var deviceArray = System.Text.Json.JsonSerializer.Deserialize<DeviceInfo[]>(json);
                    if (deviceArray != null && deviceArray.Length > 0)
                    {
                        devices.Add(deviceArray[0]);
                    }

                    File.Delete(tempPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to read device file {file.Name}: {ex.Message}");
                }
            }

            return devices.ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get registered devices: {ex.Message}");
            return Array.Empty<DeviceInfo>();
        }
    }

    /// <summary>
    /// Remove a device from sync
    /// </summary>
    public async Task UnregisterDeviceAsync(string deviceId, ICloudProvider provider)
    {
        try
        {
            // Delete device registry from cloud
            await provider.DeleteFileAsync($"/FocusDeck/sync_metadata/device_{deviceId}.json");

            // Delete device's sync data
            await provider.DeleteFileAsync($"/FocusDeck/device_data/{deviceId}/");

            System.Diagnostics.Debug.WriteLine($"Device unregistered: {deviceId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to unregister device: {ex.Message}");
        }
    }

    // Helper Methods

    /// <summary>
    /// Generate unique device ID based on MAC address and hostname
    /// </summary>
    private string GenerateDeviceId()
    {
        try
        {
            // Get MAC address of first network adapter
            string macAddress = NetworkInterface
                .GetAllNetworkInterfaces()
                .FirstOrDefault()?
                .GetPhysicalAddress()
                .ToString() ?? "UNKNOWN";

            // Combine with hostname
            string hostname = Environment.MachineName;
            string combined = $"{macAddress}_{hostname}";

            // Create hash of combined value
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(combined));
                return Convert.ToBase64String(hash)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "")
                    .Substring(0, 16); // Use first 16 characters
            }
        }
        catch
        {
            // Fallback to hostname-based ID
            return Environment.MachineName.ToLower().Replace(" ", "-");
        }
    }

    /// <summary>
    /// Generate friendly device name
    /// </summary>
    private string GenerateDeviceName()
    {
        try
        {
            string hostname = Environment.MachineName;
            string username = Environment.UserName;
            string platform = GetPlatform();
            return $"{username}'s {hostname} ({platform})";
        }
        catch
        {
            return "FocusDeck Device";
        }
    }

    /// <summary>
    /// Get platform name
    /// </summary>
    private string GetPlatform()
    {
        if (OperatingSystem.IsWindows())
            return "Windows";
        if (OperatingSystem.IsMacOS())
            return "macOS";
        if (OperatingSystem.IsLinux())
            return "Linux";
        if (OperatingSystem.IsIOS())
            return "iOS";
        if (OperatingSystem.IsAndroid())
            return "Android";
        return "Unknown";
    }
}
