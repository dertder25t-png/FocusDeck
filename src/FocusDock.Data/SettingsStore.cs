using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDock.Data.Models;

namespace FocusDock.Data;

public static class SettingsStore
{
    private static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusDock");

    private static string PathFor(string name) => System.IO.Path.Combine(Root, name);

    public static async Task<AppSettings> LoadSettingsAsync()
    {
        Directory.CreateDirectory(Root);
        var path = PathFor("settings.json");
        if (!File.Exists(path)) return new AppSettings();
        try
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public static async Task SaveSettingsAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Root);
        var path = PathFor("settings.json");
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }
    
    // Keep synchronous versions for backward compatibility
    public static AppSettings LoadSettings() => LoadSettingsAsync().GetAwaiter().GetResult();
    public static void SaveSettings(AppSettings settings) => SaveSettingsAsync(settings).GetAwaiter().GetResult();
}

