using System.Collections.Generic;

namespace FocusDock.Data.Models;

public class WindowKey
{
    public string ProcessName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty; // exact match for MVP
}

public class Workspace
{
    public string Name { get; set; } = "Untitled";
    public string? PresetName { get; set; }
    public List<WindowKey> Pinned { get; set; } = new();
}
