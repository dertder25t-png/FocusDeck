using System;
using System.IO;
using System.Text.Json;
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

    public static void Save(AutomationConfig cfg)
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
    }
}

