using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Middleware
{
    /// <summary>
    /// Authentication middleware that enforces login redirect for protected UI routes.
    /// Allows API routes (/v1/*, /swagger/*) and static assets through without authentication.
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

            // Skip authentication checks for:
            // 1. API routes (they handle their own auth via [Authorize] attributes)
            // 2. Public auth endpoints
            // 3. Static assets
            // 4. Health checks
            if (IsPublicRoute(path))
            {
                await _next(context);
                return;
            }

            // For protected UI routes, verify authentication
            var hasValidToken = HasValidAuthToken(context);

            if (!hasValidToken && IsProtectedUIRoute(path))
            {
                _logger.LogInformation("Unauthenticated request to protected route {Path}. Redirecting to /login", path);
                context.Response.Redirect("/login?redirectUrl=" + Uri.EscapeDataString(path), false);
                return;
            }

            await _next(context);
        }

        private static bool IsPublicRoute(string path)
        {
            // API routes - handled by their own [Authorize] attributes
            if (path.StartsWith("/v") && char.IsDigit(path[2]))
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

        private static bool HasValidAuthToken(HttpContext context)
        {
            // Check Authorization header
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authValue = authHeader.ToString();
                if (authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authValue.Substring("Bearer ".Length).Trim();
                    return IsValidJwt(token);
                }
            }

            // For SPA requests, also check localStorage token in cookie (if you use it)
            // This allows initial page load to check auth state
            return false;
        }

        private static bool IsValidJwt(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                
                // Check if token is expired
                if (jwtToken.ValidTo < DateTime.UtcNow)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
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
