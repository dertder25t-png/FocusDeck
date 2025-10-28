using System.Collections.Generic;
using FocusDock.SystemInterop;

namespace FocusDock.Data.Models;

public class WindowGroup
{
    public string GroupName { get; set; } = string.Empty;
    public List<WindowInfo> Windows { get; set; } = new();
}
