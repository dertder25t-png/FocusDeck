using System;

namespace FocusDeck.Domain.Entities.Sync
{
    /// <summary>
    /// Represents a durable, monotonically increasing global sync version.
    /// A new row is inserted for each change batch item; the generated Id is the version.
    /// </summary>
    public class SyncVersion : IMustHaveTenant
    {
        public long Id { get; set; } // ValueGeneratedOnAdd
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid TenantId { get; set; }
    }
}
