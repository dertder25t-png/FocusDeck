using System;
using System.IO;
using System.Text.Json;
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

    public static void SaveSettings(AppSettings settings)
    {
        Directory.CreateDirectory(Root);
        var path = PathFor("settings.json");
        File.WriteAllText(path, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}

