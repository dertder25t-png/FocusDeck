using Hangfire.Dashboard;

namespace FocusDeck.Server.Middleware;

/// <summary>
/// Authorization filter for Hangfire dashboard
/// Requires authenticated users to access the dashboard
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Require authentication for Hangfire dashboard
        return httpContext.User?.Identity?.IsAuthenticated ?? false;
    }
}
