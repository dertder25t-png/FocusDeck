using System.Runtime.InteropServices;
using FocusDeck.Shared.Models.Sync;
using FocusDeck.Shared.Services;

// Minimal headless agent to register a Linux device and poll for changes
// Configure via environment variables:
//   FOCUSDECK_SERVER_URL, FOCUSDECK_JWT (optional), FOCUSDECK_DEVICE_NAME (optional)

var serverUrl = Environment.GetEnvironmentVariable("FOCUSDECK_SERVER_URL") ?? "http://localhost:5000";
var jwt = Environment.GetEnvironmentVariable("FOCUSDECK_JWT");
var name = Environment.GetEnvironmentVariable("FOCUSDECK_DEVICE_NAME") ?? Environment.MachineName;

var deviceId = ClientSyncManager.GenerateDeviceId();
var platform = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? DevicePlatform.Linux
             : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? DevicePlatform.MacOS
             : DevicePlatform.Windows;

var client = new ClientSyncManager(serverUrl, deviceId, name, platform);
if (!string.IsNullOrWhiteSpace(jwt)) client.SetJwtToken(jwt);

Console.WriteLine($"[Agent] Registering {name} ({platform}) with ID {deviceId}");
var registered = await client.RegisterDeviceAsync();
Console.WriteLine(registered ? "[Agent] Registered" : "[Agent] Registration failed");

// Simple poll loop
while (true)
{
    try
    {
        var pull = await client.PullChangesAsync();
        if (pull.Changes.Count > 0)
        {
            Console.WriteLine($"[Agent] Pulled {pull.Changes.Count} changes; server version {pull.CurrentVersion}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Agent] Pull error: {ex.Message}");
    }

    await Task.Delay(TimeSpan.FromSeconds(60));
}
