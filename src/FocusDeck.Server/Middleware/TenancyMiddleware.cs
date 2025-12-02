using System;
using System.Threading.Tasks;
using FocusDeck.Server.Services.Tenancy;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

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

    public async Task InvokeAsync(HttpContext context, ICurrentTenant currentTenant, ITenantMembershipService tenantService)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null || endpoint.Metadata.GetMetadata<AllowAnonymousAttribute>() != null)
        {
            await _next(context);
            return;
        }

        // 1. Allow specific paths to bypass tenant checks (e.g., User Settings, Tenant Selection)
        var path = context.Request.Path.Value?.ToLower();
        if (path != null && (
            path.StartsWith("/api/v1/tenants") ||
            path.StartsWith("/api/v1/usersettings") ||
            path.StartsWith("/api/v1/onboarding")
        ))
        {
            // Proceed without setting a tenant. The Controller must handle "No Tenant" logic.
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
            // Attempt recovery: Resolve tenant from user ID
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    _logger.LogInformation("Recovering missing tenant context for user {UserId}", userId);
                    tenantId = await tenantService.EnsureTenantAsync(userId, null, null, context.RequestAborted);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to recover tenant for user {UserId}", userId);
                }
            }

            // If still invalid after recovery attempt, block
            if (tenantId == Guid.Empty)
            {
                _logger.LogWarning("Tenant context missing for authenticated request {Method} {Path}", context.Request.Method, context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Tenant context missing" });
                return;
            }
        }

        currentTenant.SetTenant(tenantId);
        await _next(context);
    }
}
