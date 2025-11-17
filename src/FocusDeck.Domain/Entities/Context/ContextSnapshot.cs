using System;
using System.Collections.Generic;

namespace FocusDeck.Domain.Entities.Context
{
    /// <summary>
    /// Represents a snapshot of the user's context at a specific point in time.
    /// </summary>
    public class ContextSnapshot
    {
        /// <summary>
        /// Gets or sets the unique identifier for the snapshot.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID associated with this snapshot.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the snapshot was taken.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the collection of context slices that make up this snapshot.
        /// </summary>
        public List<ContextSlice> Slices { get; set; } = new List<ContextSlice>();

        /// <summary>
        /// Gets or sets the metadata associated with this snapshot.
        /// </summary>
        public ContextSnapshotMetadata? Metadata { get; set; }
    }
}
