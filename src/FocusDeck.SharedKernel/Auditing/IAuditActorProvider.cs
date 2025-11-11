namespace FocusDeck.SharedKernel.Auditing;

public interface IAuditActorProvider
{
    string? GetActorId();
}
