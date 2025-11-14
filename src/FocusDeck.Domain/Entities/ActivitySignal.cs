using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Domain.Entities;

public sealed class ActivitySignal : IMustHaveTenant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public string SignalValue { get; set; } = string.Empty;
    public DateTime CapturedAtUtc { get; set; }
    public string SourceApp { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}
