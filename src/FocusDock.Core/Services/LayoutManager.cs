// Project Snapshot - October 29, 2025
using System;
using System.Collections.Generic;
using System.Diagnostics;
using FocusDock.Data.Models;
using FocusDock.SystemInterop;

namespace FocusDock.Core.Services;

public class LayoutManager
{
    public void ApplyPreset(LayoutPreset preset)
    {
        // Get current windows and assign them to zones in order
        var tracker = new WindowTracker();
        var windows = tracker.GetCurrentWindows();
        var zones = preset.Zones;

        if (zones.Count == 0) return;
        int zi = 0;

        // Using resolution provided in preset
        foreach (var w in windows)
        {
            var z = zones[zi % zones.Count];
            int x = (int)Math.Round(z.X * preset.ScreenWidth);
            int y = (int)Math.Round(z.Y * preset.ScreenHeight);
            int width = (int)Math.Round(z.Width * preset.ScreenWidth);
            int height = (int)Math.Round(z.Height * preset.ScreenHeight);

            try
            {
                User32.SetWindowPos(w.Hwnd, IntPtr.Zero, x, y, width, height,
                    SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_SHOWWINDOW);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to position window {w.Title}: {ex.Message}");
            }
            zi++;
        }
    }

    public void ApplyPresetForBounds(LayoutPreset preset, int targetWidth, int targetHeight)
    {
        // Clone zones but scale to target dimensions
        var scaled = new LayoutPreset
        {
            Name = preset.Name,
            ScreenWidth = targetWidth,
            ScreenHeight = targetHeight,
            Zones = preset.Zones
        };
        ApplyPreset(scaled);
    }
}
