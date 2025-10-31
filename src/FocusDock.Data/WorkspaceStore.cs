using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDock.Data.Models;

namespace FocusDock.Data;

public static class WorkspaceStore
{
    private static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FocusDock");

    private static string FilePath => Path.Combine(Root, "workspaces.json");

    public static List<Workspace> LoadAll()
    {
        Directory.CreateDirectory(Root);
        if (!File.Exists(FilePath)) return new List<Workspace>();
        try
        {
            return JsonSerializer.Deserialize<List<Workspace>>(File.ReadAllText(FilePath)) ?? new List<Workspace>();
        }
        catch { return new List<Workspace>(); }
    }
    
    /// <summary>
    /// Async version of LoadAll for non-blocking file I/O
    /// </summary>
    public static async Task<List<Workspace>> LoadAllAsync()
    {
        Directory.CreateDirectory(Root);
        if (!File.Exists(FilePath)) return new List<Workspace>();
        try
        {
            var json = await File.ReadAllTextAsync(FilePath);
            return JsonSerializer.Deserialize<List<Workspace>>(json) ?? new List<Workspace>();
        }
        catch { return new List<Workspace>(); }
    }

    public static void SaveOrUpdate(Workspace ws)
    {
        var all = LoadAll();
        var idx = all.FindIndex(x => x.Name == ws.Name);
        if (idx >= 0) all[idx] = ws; else all.Add(ws);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
    }
    
    /// <summary>
    /// Async version of SaveOrUpdate for non-blocking file I/O
    /// </summary>
    public static async Task SaveOrUpdateAsync(Workspace ws)
    {
        var all = await LoadAllAsync();
        var idx = all.FindIndex(x => x.Name == ws.Name);
        if (idx >= 0) all[idx] = ws; else all.Add(ws);
        await File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true }));
    }
}


