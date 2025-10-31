using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDock.Data.Models;

namespace FocusDock.Data;

public static class LocalStore
{
    private static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusDock");

    public static async Task SavePresetsAsync(IEnumerable<LayoutPreset> presets)
    {
        Directory.CreateDirectory(Root);
        var path = Path.Combine(Root, "presets.json");
        var json = JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    public static async Task<List<LayoutPreset>> LoadPresetsAsync()
    {
        var path = Path.Combine(Root, "presets.json");
        if (!File.Exists(path)) return new List<LayoutPreset>();
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<LayoutPreset>>(json) ?? new List<LayoutPreset>();
    }
    
    // Keep synchronous versions for backward compatibility
    public static void SavePresets(IEnumerable<LayoutPreset> presets)
    {
        SavePresetsAsync(presets).GetAwaiter().GetResult();
    }

    public static List<LayoutPreset> LoadPresets()
    {
        return LoadPresetsAsync().GetAwaiter().GetResult();
    }
}

