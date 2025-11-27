using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FocusDeck.Server.Services.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace FocusDeck.Server.Middleware
{
    /// <summary>
    /// Authentication middleware that enforces login redirect for protected UI routes.
    /// Allows API routes (/v1/*, /swagger/*) and static assets through without authentication.
    /// </summary>
    public class AuthenticationMiddleware
    {
        private const string AccessCookieName = "focusdeck_access_token";
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;
        private readonly TokenValidationParameters _validationParameters;
        private readonly JwtSecurityTokenHandler _tokenHandler = new();

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger, TokenValidationParameters validationParameters)
        {
            _next = next;
            _logger = logger;
            _validationParameters = validationParameters;
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

            if (!TryValidateToken(context, out var principal, out var validationOutcome))
            {
                var reason = GetOutcomeReason(validationOutcome);
                AuthTelemetry.RecordJwtValidationFailure(reason);
                _logger.LogWarning("JWT validation failed ({Reason}) for path {Path} from {RemoteIp}", reason, path, context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
                if (IsProtectedUIRoute(path))
                {
                    context.Response.Redirect("/login?redirectUrl=" + Uri.EscapeDataString(path), false);
                    return;
                }

                context.Response.StatusCode = validationOutcome == TokenValidationOutcome.MissingTenant
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Authentication required.");
                return;
            }

            if (principal != null)
            {
                context.User = principal;
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

        private static string GetOutcomeReason(TokenValidationOutcome outcome) => outcome switch
        {
            TokenValidationOutcome.MissingToken => "missing-token",
            TokenValidationOutcome.MissingTenant => "missing-tenant",
            TokenValidationOutcome.Expired => "expired",
            TokenValidationOutcome.Invalid => "invalid",
            _ => "unknown"
        };

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

        private bool TryValidateToken(HttpContext context, out ClaimsPrincipal? principal, out TokenValidationOutcome outcome)
        {
            principal = null;
            outcome = TokenValidationOutcome.None;

            if (!TryExtractToken(context, out var token))
            {
                outcome = TokenValidationOutcome.MissingToken;
                return false;
            }

            try
            {
                principal = _tokenHandler.ValidateToken(token, _validationParameters, out _);
            }
            catch (SecurityTokenExpiredException)
            {
                outcome = TokenValidationOutcome.Expired;
                return false;
            }
            catch (SecurityTokenException)
            {
                outcome = TokenValidationOutcome.Invalid;
                return false;
            }
            catch
            {
                outcome = TokenValidationOutcome.Invalid;
                return false;
            }

            var tenantClaim = principal.FindFirst("app_tenant_id")?.Value;
            if (string.IsNullOrWhiteSpace(tenantClaim) || !Guid.TryParse(tenantClaim, out _))
            {
                outcome = TokenValidationOutcome.MissingTenant;
                return false;
            }

            outcome = TokenValidationOutcome.Valid;
            return true;
        }

        private static bool TryExtractToken(HttpContext context, out string? token)
        {
            token = null;

            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var authValue = authHeader.ToString();
                if (authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    token = authValue.Substring("Bearer ".Length).Trim();
                    if (!string.IsNullOrEmpty(token))
                    {
                        return true;
                    }
                }
            }

            if (context.Request.Cookies.TryGetValue(AccessCookieName, out var cookieToken) &&
                !string.IsNullOrWhiteSpace(cookieToken))
            {
                token = cookieToken;
                return true;
            }

            return false;
        }

        private enum TokenValidationOutcome
        {
            None,
            Valid,
            MissingToken,
            MissingTenant,
            Expired,
            Invalid
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
