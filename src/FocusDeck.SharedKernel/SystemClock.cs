namespace FocusDeck.SharedKernel;

/// <summary>
/// System clock implementation that returns the current UTC time.
/// </summary>
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
