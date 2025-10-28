using System.Collections.Generic;
using FocusDock.Data.Models;

namespace FocusDock.Data;

public static class PresetService
{
    public static List<LayoutPreset> Load() => LocalStore.LoadPresets();

    public static void SaveOrUpdate(LayoutPreset preset)
    {
        var all = LocalStore.LoadPresets();
        var idx = all.FindIndex(p => p.Name == preset.Name);
        if (idx >= 0) all[idx] = preset; else all.Add(preset);
        LocalStore.SavePresets(all);
    }
}

