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

    public static AppSettings LoadSettings()
    {
        Directory.CreateDirectory(Root);
        var path = PathFor("settings.json");
        if (!File.Exists(path)) return new AppSettings();
        try
        {
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }
    
    /// <summary>
    /// Async version of LoadSettings for non-blocking file I/O
    /// </summary>
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

    public static void SaveSettings(AppSettings settings)
    {
        Directory.CreateDirectory(Root);
        var path = PathFor("settings.json");
        File.WriteAllText(path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
    
    /// <summary>
    /// Async version of SaveSettings for non-blocking file I/O
    /// </summary>
    public static async Task SaveSettingsAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Root);
        var path = PathFor("settings.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}


