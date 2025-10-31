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

    public static void SavePresets(IEnumerable<LayoutPreset> presets)
    {
        Directory.CreateDirectory(Root);
        var path = Path.Combine(Root, "presets.json");
        var json = JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
    
    /// <summary>
    /// Async version of SavePresets for non-blocking file I/O
    /// </summary>
    public static async Task SavePresetsAsync(IEnumerable<LayoutPreset> presets)
    {
        Directory.CreateDirectory(Root);
        var path = Path.Combine(Root, "presets.json");
        var json = JsonSerializer.Serialize(presets, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    public static List<LayoutPreset> LoadPresets()
    {
        var path = Path.Combine(Root, "presets.json");
        if (!File.Exists(path)) return new List<LayoutPreset>();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<LayoutPreset>>(json) ?? new List<LayoutPreset>();
    }
    
    /// <summary>
    /// Async version of LoadPresets for non-blocking file I/O
    /// </summary>
    public static async Task<List<LayoutPreset>> LoadPresetsAsync()
    {
        var path = Path.Combine(Root, "presets.json");
        if (!File.Exists(path)) return new List<LayoutPreset>();
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<List<LayoutPreset>>(json) ?? new List<LayoutPreset>();
    }
}


