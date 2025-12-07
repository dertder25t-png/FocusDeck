using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Middleware
{
    /// <summary>
    /// Authentication middleware that enforces login redirect for protected UI routes.
    /// Allows API routes (/v1/*, /swagger/*) and static assets through without authentication.
    /// Works with cookie-based authentication.
    /// </summary>
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";

            // Skip authentication checks for known public routes (login, register, auth, static assets, health)
            if (IsPublicRoute(path))
            {
                await _next(context);
                return;
            }

            // Check if user is authenticated via cookie
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("Unauthenticated request for path {Path} from {RemoteIp}", path, context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                
                if (IsProtectedUIRoute(path))
                {
                    context.Response.Redirect("/login?redirectUrl=" + Uri.EscapeDataString(path), false);
                    return;
                }

                // For API routes, return 401
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Authentication required.");
                return;
            }

            // Verify tenant claim exists
            var tenantClaim = context.User.FindFirst("app_tenant_id")?.Value;
            if (string.IsNullOrWhiteSpace(tenantClaim) || !Guid.TryParse(tenantClaim, out _))
            {
                _logger.LogWarning("Missing or invalid tenant claim for authenticated user on path {Path}", path);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Tenant information required.");
                return;
            }

            await _next(context);
        }

        private static bool IsPublicRoute(string path)
        {
            // API routes - handled by their own [Authorize] attributes
            if (path.StartsWith("/v") && path.Length > 2 && char.IsDigit(path[2]))
                return true;

            // Swagger/OpenAPI
            if (path.StartsWith("/swagger") || path.StartsWith("/api/"))
                return true;

            // Health checks
            if (path.StartsWith("/health"))
                return true;

            // Static files with known extensions
            if (path.EndsWith(".js") || path.EndsWith(".css") || path.EndsWith(".png") ||
                path.EndsWith(".jpg") || path.EndsWith(".gif") || path.EndsWith(".svg") ||
                path.EndsWith(".woff") || path.EndsWith(".woff2") || path.EndsWith(".ttf") ||
                path.EndsWith(".json") || path.EndsWith(".ico") || path.EndsWith(".map"))
                return true;

            // Public auth pages
            if (path == "/login" || path == "/register" || path.StartsWith("/auth/"))
                return true;

            // Root path is allowed (will redirect via React Router)
            if (path == "/" || path == "")
                return true;

            return false;
        }

        private static bool IsProtectedUIRoute(string path)
        {
            // Any route that's not public and not login/register
            return !path.StartsWith("/v") &&
                   !path.StartsWith("/swagger") &&
                   !path.StartsWith("/health") &&
                   !path.StartsWith("/api/") &&
                   path != "/login" &&
                   path != "/register" &&
                   !path.EndsWith(".js") &&
                   !path.EndsWith(".css") &&
                   !path.EndsWith(".json");
        }
    }

    /// <summary>
    /// Extension method to add authentication middleware to the pipeline.
    /// </summary>
    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}
