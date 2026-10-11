using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace ReelSoon.Services.Auth;

/// <summary>
/// Resolves the caller from <c>HttpContext</c> when there is one, and from the Blazor
/// authentication state otherwise. Interactive Blazor Server components run on a SignalR
/// circuit where <c>HttpContext</c> is null, so an HTTP-only implementation would report every
/// component interaction as anonymous.
/// </summary>
public sealed class BlazorAwareUserContextService(
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider) : IUserContextService
{
    public string? GetCurrentUserId() => ResolvePrincipal().FindUserId();

    public string? GetCurrentUserName() => ResolvePrincipal()?.Identity?.Name;

    public bool IsAuthenticated() => ResolvePrincipal()?.Identity?.IsAuthenticated ?? false;

    public bool IsAdministrator() => ResolvePrincipal()?.IsInRole(Constants.Roles.Administrator) ?? false;

    public IReadOnlyList<string> GetRoleNames()
    {
        var user = ResolvePrincipal();

        return user is null
            ? []
            : user.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    private ClaimsPrincipal? ResolvePrincipal()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            return httpContext.User;
        }

        try
        {
            // Sync-over-async is unavoidable here: IUserContextService is consumed from
            // synchronous property getters, and the state provider's task is already completed
            // for an established circuit.
            var user = Task.Run(async () =>
                    (await authenticationStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false)).User)
                .GetAwaiter()
                .GetResult();

            return user.Identity?.IsAuthenticated == true ? user : httpContext?.User;
        }
        catch
        {
            return httpContext?.User;
        }
    }
}
