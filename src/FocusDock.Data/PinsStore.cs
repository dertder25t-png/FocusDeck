using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDock.Data.Models;

namespace FocusDock.Data;

public static class PinsStore
{
    private static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusDock");

    private static string FilePath => Path.Combine(Root, "pins.json");

    public static HashSet<(string Process, string Title)> Load()
    {
        Directory.CreateDirectory(Root);
        if (!File.Exists(FilePath)) return new HashSet<(string, string)>();
        try
        {
            var list = JsonSerializer.Deserialize<List<WindowKey>>(File.ReadAllText(FilePath)) ?? new();
            return new HashSet<(string, string)>(list.ConvertAll(k => (k.ProcessName, k.Title)));
        }
        catch { return new HashSet<(string, string)>(); }
    }
    
    /// <summary>
    /// Async version of Load for non-blocking file I/O
    /// </summary>
    public static async Task<HashSet<(string Process, string Title)>> LoadAsync()
    {
        Directory.CreateDirectory(Root);
        if (!File.Exists(FilePath)) return new HashSet<(string, string)>();
        try
        {
            var json = await File.ReadAllTextAsync(FilePath);
            var list = JsonSerializer.Deserialize<List<WindowKey>>(json) ?? new();
            return new HashSet<(string, string)>(list.ConvertAll(k => (k.ProcessName, k.Title)));
        }
        catch { return new HashSet<(string, string)>(); }
    }

    public static void Save(HashSet<(string Process, string Title)> set)
    {
        Directory.CreateDirectory(Root);
        var list = new List<WindowKey>();
        foreach (var (p, t) in set) list.Add(new WindowKey { ProcessName = p, Title = t });
        File.WriteAllText(FilePath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
    }
    
    /// <summary>
    /// Async version of Save for non-blocking file I/O
    /// </summary>
    public static async Task SaveAsync(HashSet<(string Process, string Title)> set)
    {
        Directory.CreateDirectory(Root);
        var list = new List<WindowKey>();
        foreach (var (p, t) in set) list.Add(new WindowKey { ProcessName = p, Title = t });
        await File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
    }
}


