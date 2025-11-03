namespace FocusDeck.SharedKernel;

/// <summary>
/// Generates unique identifiers for entities.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Generates a new unique identifier.
    /// </summary>
    Guid NewId();
}
