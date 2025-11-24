using System;
using System.Collections.Generic;

namespace FocusDeck.Domain.Entities
{
    public class BrowserSession : IMustHaveTenant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DeviceId { get; set; } = string.Empty;
        public Guid TenantId { get; set; }

        // JSON serialized list of tabs
        public string TabsJson { get; set; } = "[]";

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public Guid? BoundProjectId { get; set; }
        public Project? BoundProject { get; set; }
    }

    public class TabItem
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
