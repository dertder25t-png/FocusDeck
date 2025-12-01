using FocusDeck.SharedKernel.Tenancy;

namespace FocusDeck.Server.Services.Tenancy;

// A simple implementation for background jobs to manually set the scope
public class BackgroundWorkerTenant : ICurrentTenant
{
    private Guid? _tenantId;

    public Guid? TenantId => _tenantId;
    public bool HasTenant => _tenantId.HasValue && _tenantId != Guid.Empty;

    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
    }
}
