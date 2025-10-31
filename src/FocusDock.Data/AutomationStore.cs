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

    public static AutomationConfig Load()
    {
        Directory.CreateDirectory(Root);
        if (!File.Exists(FilePath)) return new AutomationConfig();
        try
        {
            return JsonSerializer.Deserialize<AutomationConfig>(File.ReadAllText(FilePath)) ?? new AutomationConfig();
        }
        catch { return new AutomationConfig(); }
    }
    
    /// <summary>
    /// Async version of Load for non-blocking file I/O
    /// </summary>
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

    public static void Save(AutomationConfig cfg)
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
    }
    
    /// <summary>
    /// Async version of Save for non-blocking file I/O
    /// </summary>
    public static async Task SaveAsync(AutomationConfig cfg)
    {
        Directory.CreateDirectory(Root);
        await File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
    }
}


