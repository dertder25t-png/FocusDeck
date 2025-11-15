using System;
using System.Threading.Tasks;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Middleware;

public sealed class TenancyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenancyMiddleware> _logger;

    public TenancyMiddleware(RequestDelegate next, ILogger<TenancyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null || endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var tenantClaim = context.User.FindFirst("app_tenant_id")?.Value
                          ?? context.User.FindFirst("tenant_id")?.Value;

        if (!Guid.TryParse(tenantClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            _logger.LogWarning("Tenant context missing for authenticated request {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Tenant context missing" });
            return;
        }

        currentTenant.SetTenant(tenantId);
        await _next(context);
    }
}
