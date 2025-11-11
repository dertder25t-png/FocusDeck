using FocusDeck.SharedKernel.Auditing;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace FocusDeck.Server.Services.Auditing;

public sealed class HttpContextAuditActorProvider : IAuditActorProvider
{
    private readonly IHttpContextAccessor _contextAccessor;

    public HttpContextAuditActorProvider(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public string? GetActorId()
        => _contextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
