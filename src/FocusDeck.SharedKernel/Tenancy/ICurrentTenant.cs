using System;

namespace FocusDeck.SharedKernel.Tenancy;

public interface ICurrentTenant
{
    Guid? TenantId { get; }
    bool HasTenant { get; }
    void SetTenant(Guid tenantId);
}
