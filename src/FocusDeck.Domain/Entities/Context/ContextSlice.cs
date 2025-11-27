using System;
using System.Text.Json.Nodes;

namespace FocusDeck.Domain.Entities.Context
{
    /// <summary>
    /// Represents a slice of context from a specific source.
    /// </summary>
    public class ContextSlice
    {
        /// <summary>
        /// Gets or sets the unique identifier for the slice.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the snapshot this slice belongs to.
        /// </summary>
        public Guid SnapshotId { get; set; }

        /// <summary>
        /// Gets or sets the type of the source that generated this slice.
        /// </summary>
        public ContextSourceType SourceType { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the slice was captured.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the data payload of the slice as a JSON object.
        /// </summary>
        public JsonObject? Data { get; set; }
    }
}
