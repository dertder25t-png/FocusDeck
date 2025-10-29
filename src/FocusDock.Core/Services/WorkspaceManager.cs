// Project Snapshot - October 29, 2025
using System.Collections.Generic;
using System.Linq;
using FocusDock.Data.Models;
using FocusDock.SystemInterop;

namespace FocusDock.Core.Services;

public class WorkspaceManager
{
    private readonly PinService _pins;

    public WorkspaceManager(PinService pins)
    {
        _pins = pins;
    }

    public Workspace CaptureCurrent(string name, string? presetName = null)
    {
        var tracker = new WindowTracker();
        var windows = tracker.GetCurrentWindows();
        var pinned = windows.Where(w => _pins.IsPinned(w))
            .Select(w => new WindowKey { ProcessName = w.ProcessName, Title = w.Title })
            .ToList();
        return new Workspace
        {
            Name = name,
            PresetName = presetName,
            Pinned = pinned
        };
    }

    public void Restore(Workspace ws)
    {
        var tracker = new WindowTracker();
        var current = tracker.GetCurrentWindows();
        foreach (var key in ws.Pinned)
        {
            var match = FindBestMatch(current, key);
            if (match != null) _pins.SetPinned(match, true);
        }
    }

    private static WindowInfo? FindBestMatch(System.Collections.Generic.List<WindowInfo> current, WindowKey key)
    {
        // 1) Exact match
        var exact = current.FirstOrDefault(w => w.ProcessName == key.ProcessName && w.Title == key.Title);
        if (exact != null) return exact;

        // 2) Case-insensitive contains on normalized titles for same process
        string norm(string s) => new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        var ktitle = norm(key.Title);
        var contains = current.FirstOrDefault(w => w.ProcessName == key.ProcessName && (norm(w.Title).Contains(ktitle) || ktitle.Contains(norm(w.Title))));
        if (contains != null) return contains;

        // 3) Fuzzy via Levenshtein similarity for same process
        WindowInfo? best = null;
        double bestScore = 0;
        foreach (var w in current.Where(w => w.ProcessName == key.ProcessName))
        {
            var score = Similarity(norm(w.Title), ktitle);
            if (score > bestScore)
            {
                bestScore = score; best = w;
            }
        }
        return bestScore >= 0.65 ? best : null;
    }

    private static double Similarity(string a, string b)
    {
        if (a.Length == 0 && b.Length == 0) return 1;
        int d = Levenshtein(a, b);
        int m = Math.Max(a.Length, b.Length);
        return 1.0 - (double)d / m;
    }

    private static int Levenshtein(string s, string t)
    {
        var n = s.Length; var m = t.Length;
        var dp = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) dp[i, 0] = i;
        for (int j = 0; j <= m; j++) dp[0, j] = j;
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }
        return dp[n, m];
    }
}
