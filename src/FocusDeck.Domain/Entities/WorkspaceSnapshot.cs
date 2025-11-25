using System;
using System.Collections.Generic;

namespace FocusDeck.Domain.Entities
{
    public class WorkspaceSnapshot : IMustHaveTenant
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public Guid TenantId { get; set; }
        public string Name { get; set; } = "Untitled Snapshot";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Layout Data (JSON)
        public string WindowLayoutJson { get; set; } = "{}";

        // Linked Context
        public string? BrowserSessionId { get; set; }
        public BrowserSession? BrowserSession { get; set; }

        public string? ActiveNoteId { get; set; }
        public Note? ActiveNote { get; set; }

        public Guid? ProjectId { get; set; }
        public Project? Project { get; set; }
    }
}
