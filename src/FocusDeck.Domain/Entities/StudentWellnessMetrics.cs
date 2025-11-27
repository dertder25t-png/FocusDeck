using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Domain.Entities;

public sealed class StudentWellnessMetrics : IMustHaveTenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;

    public double HoursWorked { get; set; }
    public double BreakFrequency { get; set; }
    public double QualityScore { get; set; }
    public double SleepHours { get; set; }
    public bool IsUnsustainable { get; set; }

    public string? Notes { get; set; }
}
