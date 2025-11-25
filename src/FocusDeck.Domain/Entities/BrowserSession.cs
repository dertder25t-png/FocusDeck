using System;
using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Domain.Entities
{
    public class BrowserSession : IMustHaveTenant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public Guid TenantId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndedAt { get; set; }
        public string DeviceId { get; set; } = string.Empty;

        // JSON representation of open tabs
        public string TabsJson { get; set; } = "[]";
    }
}
