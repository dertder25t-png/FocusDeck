using FocusDeck.SharedKernel.Tenancy;
using System;
using System.Collections.Generic;

namespace FocusDeck.Domain.Entities;

public class Habit : IMustHaveTenant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = "fa-star"; // FontAwesome icon class
    public string Frequency { get; set; } = "daily"; // daily, weekly
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<HabitCompletion> Completions { get; set; } = new List<HabitCompletion>();
}
