using System.Collections.Generic;

namespace FocusDock.Data.Models;

public class LayoutZone
{
    // normalized 0..1
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class LayoutPreset
{
    public string Name { get; set; } = "Untitled";
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
    public List<LayoutZone> Zones { get; set; } = new();

    public static LayoutPreset CreateTwoColumn(int width, int height)
    {
        return new LayoutPreset
        {
            Name = "Two Column",
            ScreenWidth = width,
            ScreenHeight = height,
            Zones = new List<LayoutZone>
            {
                new LayoutZone { X = 0, Y = 0, Width = 0.5, Height = 1.0 },
                new LayoutZone { X = 0.5, Y = 0, Width = 0.5, Height = 1.0 }
            }
        };
    }

    public static LayoutPreset CreateThreeColumn(int width, int height)
    {
        return new LayoutPreset
        {
            Name = "Three Column",
            ScreenWidth = width,
            ScreenHeight = height,
            Zones = new List<LayoutZone>
            {
                new LayoutZone { X = 0.0,  Y = 0.0, Width = 1.0/3.0, Height = 1.0 },
                new LayoutZone { X = 1.0/3.0, Y = 0.0, Width = 1.0/3.0, Height = 1.0 },
                new LayoutZone { X = 2.0/3.0, Y = 0.0, Width = 1.0/3.0, Height = 1.0 },
            }
        };
    }

    public static LayoutPreset CreateGrid2x2(int width, int height)
    {
        return new LayoutPreset
        {
            Name = "Grid 2x2",
            ScreenWidth = width,
            ScreenHeight = height,
            Zones = new List<LayoutZone>
            {
                new LayoutZone { X = 0.0, Y = 0.0, Width = 0.5, Height = 0.5 },
                new LayoutZone { X = 0.5, Y = 0.0, Width = 0.5, Height = 0.5 },
                new LayoutZone { X = 0.0, Y = 0.5, Width = 0.5, Height = 0.5 },
                new LayoutZone { X = 0.5, Y = 0.5, Width = 0.5, Height = 0.5 },
            }
        };
    }
}
