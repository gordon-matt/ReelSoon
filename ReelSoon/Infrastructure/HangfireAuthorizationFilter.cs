using Hangfire.Dashboard;

namespace ReelSoon.Infrastructure;

/// <summary>Restricts the Hangfire dashboard to authenticated administrators.</summary>
public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole(Constants.Roles.Administrator);
    }
}
