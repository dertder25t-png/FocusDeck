using System;

namespace FocusDeck.SharedKernel.Tenancy;

public sealed class NullCurrentTenant : ICurrentTenant
{
    public static readonly NullCurrentTenant Instance = new();

    public Guid? TenantId => null;
    public bool HasTenant => false;

    public void SetTenant(Guid tenantId)
    {
        // No-op: null tenant represents a non-scoped context.
    }
}
