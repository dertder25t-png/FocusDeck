namespace FocusDeck.SharedKernel;

/// <summary>
/// Default ID generator that creates new GUIDs.
/// </summary>
public class GuidIdGenerator : IIdGenerator
{
    public Guid NewId() => Guid.NewGuid();
}
