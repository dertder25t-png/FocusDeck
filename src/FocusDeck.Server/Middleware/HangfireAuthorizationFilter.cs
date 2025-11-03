using Hangfire.Dashboard;

namespace FocusDeck.Server.Middleware;

/// <summary>
/// Authorization filter for Hangfire dashboard
/// Requires authenticated users with Admin role to access the dashboard
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Require authentication AND Admin role for Hangfire dashboard
        var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated ?? false;
        if (!isAuthenticated || httpContext.User == null)
        {
            return false;
        }

        // Check for Admin role or admin claim
        var isAdmin = httpContext.User.IsInRole("Admin") || 
                     httpContext.User.HasClaim("role", "admin") ||
                     httpContext.User.HasClaim("admin", "true");

        return isAdmin;
    }
}
