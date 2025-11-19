using System;
using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Domain.Entities.Context
{
    public class ContextVector : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid SnapshotId { get; set; }
        public byte[] VectorData { get; set; } = Array.Empty<byte>();
        public int Dimensions { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }

        // Navigation property
        public ContextSnapshot? Snapshot { get; set; }
    }
}
