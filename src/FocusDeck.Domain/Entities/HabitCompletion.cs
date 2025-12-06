using System;

namespace FocusDeck.Domain.Entities;

public class HabitCompletion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string HabitId { get; set; } = string.Empty;
    public DateTime Date { get; set; } // The date of completion (not exact time)
    
    public Habit? Habit { get; set; }
}
