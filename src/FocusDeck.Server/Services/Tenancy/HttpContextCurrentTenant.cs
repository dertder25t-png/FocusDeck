using FocusDeck.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Http;

namespace FocusDeck.Server.Services.Tenancy;

public sealed class HttpContextCurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _tenantId;

    public HttpContextCurrentTenant(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId
    {
        get
        {
            if (_tenantId.HasValue)
            {
                return _tenantId;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var claimValue = httpContext?.User?.FindFirst("app_tenant_id")?.Value
                ?? httpContext?.User?.FindFirst("tenant_id")?.Value;

            if (Guid.TryParse(claimValue, out var tenantId))
            {
                _tenantId = tenantId;
                return tenantId;
            }

            return _tenantId;
        }
    }

    public bool HasTenant => TenantId.HasValue && TenantId != Guid.Empty;

    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
    }
}
