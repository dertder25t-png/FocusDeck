using System;
using FocusDeck.Domain.Entities;

namespace FocusDeck.Domain.Entities.Remote
{
    public enum JobStatus
    {
        Queued = 0,
        Dispatched = 1,
        Running = 2,
        Completed = 3,
        Failed = 4,
        Expired = 5
    }

    public class DeviceJob : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public string TargetDeviceId { get; set; } = string.Empty;
        public string JobType { get; set; } = string.Empty; // e.g., "arrange_layout", "open_url"
        public string PayloadJson { get; set; } = "{}";
        public JobStatus Status { get; set; } = JobStatus.Queued;
        public string? ResultJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public DateTime ExpiresAt { get; set; }

        public string UserId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
    }
}
