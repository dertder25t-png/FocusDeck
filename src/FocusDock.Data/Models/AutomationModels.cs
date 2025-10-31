using System;
using System.Collections.Generic;

namespace FocusDock.Data.Models;

public enum RuleTriggerType
{
    Time,
    WiFiNetwork,
    ApplicationFocus
}

public enum RuleAction
{
    ApplyPreset,
    FocusModeOn,
    FocusModeOff
}

public class TimeRule
{
    public string Name { get; set; } = "Rule";
    public RuleTriggerType TriggerType { get; set; } = RuleTriggerType.Time;
    
    // Time-based trigger properties
    public IEnumerable<DayOfWeek> DaysOfWeek { get; set; } = Array.Empty<DayOfWeek>();
    public string Start { get; set; } = "00:00"; // HH:mm
    public string End { get; set; } = "23:59";   // HH:mm
    
    // WiFi-based trigger properties
    public string? WiFiSSID { get; set; }
    public bool OnConnect { get; set; } = true; // true = on connect, false = on disconnect
    
    // App-based trigger properties
    public string? ApplicationName { get; set; } // e.g., "Visual Studio Code"
    public string? ProcessName { get; set; }     // e.g., "Code.exe"
    
    // Action properties
    public RuleAction Action { get; set; }
    public string? PresetName { get; set; }
    public int? MonitorIndex { get; set; }
    
    public bool IsEnabled { get; set; } = true;
}

public class AutomationConfig
{
    public List<TimeRule> Rules { get; set; } = new();
}
