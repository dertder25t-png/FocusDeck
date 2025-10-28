using System;
using System.Collections.Generic;

namespace FocusDock.Data.Models;

public enum RuleAction
{
    ApplyPreset,
    FocusModeOn,
    FocusModeOff
}

public class TimeRule
{
    public string Name { get; set; } = "Rule";
    public IEnumerable<DayOfWeek> DaysOfWeek { get; set; } = Array.Empty<DayOfWeek>();
    public string Start { get; set; } = "00:00"; // HH:mm
    public string End { get; set; } = "23:59";   // HH:mm
    public RuleAction Action { get; set; }
    public string? PresetName { get; set; }
    public int? MonitorIndex { get; set; }
}

public class AutomationConfig
{
    public List<TimeRule> Rules { get; set; } = new();
}
