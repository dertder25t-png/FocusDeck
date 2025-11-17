namespace FocusDeck.Domain.Entities.Context
{
    /// <summary>
    /// Represents metadata associated with a context snapshot.
    /// </summary>
    public class ContextSnapshotMetadata
    {
        /// <summary>
        /// Gets or sets the name of the device that captured the snapshot.
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the operating system of the device.
        /// </summary>
        public string? OperatingSystem { get; set; }
    }
}
