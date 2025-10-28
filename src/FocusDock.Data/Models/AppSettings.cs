namespace FocusDock.Data.Models;

public enum DockEdge
{
    Top,
    Bottom,
    Left,
    Right
}

public class AppSettings
{
    public int MonitorIndex { get; set; } = 0;
    public DockEdge Edge { get; set; } = DockEdge.Top;
}
