namespace FocusDock.Data.Models;

public enum DockEdge
{
    Top,
    Bottom,
    Left,
    Right
}

public enum DockAlignment
{
    Left,
    Center,
    Right
}

public class AppSettings
{
    public string? ServerUrl { get; set; }
    public int MonitorIndex { get; set; } = 0;
    public DockEdge Edge { get; set; } = DockEdge.Top;
    public DockAlignment Alignment { get; set; } = DockAlignment.Center;
}
