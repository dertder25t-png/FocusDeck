using System;
using System.Linq;
using FocusDock.Data.Models;

namespace FocusDock.Core.Services;

public class AutomationService
{
    private readonly System.Timers.Timer _timer = new(60_000); // every minute
    public AutomationConfig Config { get; set; } = new();

    public event EventHandler<TimeRule>? RuleTriggered;

    public AutomationService()
    {
        _timer.Elapsed += (_, _) => Tick();
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();

    private void Tick()
    {
        var now = DateTime.Now;
        foreach (var rule in Config.Rules)
        {
            if (!rule.DaysOfWeek.Contains(now.DayOfWeek)) continue;
            if (!TryParse(rule.Start, out var start) || !TryParse(rule.End, out var end)) continue;
            var within = now.TimeOfDay >= start && now.TimeOfDay <= end;
            if (!within) continue;

            RuleTriggered?.Invoke(this, rule);
        }
    }

    private static bool TryParse(string hhmm, out TimeSpan ts)
    {
        ts = default;
        var parts = hhmm.Split(':');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], out var h)) return false;
        if (!int.TryParse(parts[1], out var m)) return false;
        ts = new TimeSpan(h, m, 0);
        return true;
    }
}
