namespace FocusDeck.SharedKernel;

/// <summary>
/// Provides access to the current date and time.
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC date and time.
    /// </summary>
    DateTime UtcNow { get; }
}
