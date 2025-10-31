using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDock.Data.Models;

namespace FocusDock.Data;

public static class AutomationStore
{
    private static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusDock");

    private static string FilePath => Path.Combine(Root, "automation.json");

    public static async Task<AutomationConfig> LoadAsync()
    {
        Directory.CreateDirectory(Root);
        if (!File.Exists(FilePath)) return new AutomationConfig();
        try
        {
            var json = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<AutomationConfig>(json) ?? new AutomationConfig();
        }
        catch { return new AutomationConfig(); }
    }

    public static async Task SaveAsync(AutomationConfig cfg)
    {
        Directory.CreateDirectory(Root);
        var json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(FilePath, json);
    }
    
    // Keep synchronous versions for backward compatibility
    public static AutomationConfig Load() => LoadAsync().GetAwaiter().GetResult();
    public static void Save(AutomationConfig cfg) => SaveAsync(cfg).GetAwaiter().GetResult();
}

