using System.Security.Claims;

namespace ReelSoon.Services.Auth;

/// <summary>
/// Keycloak's user id lives on <c>sub</c>. ASP.NET maps that to
/// <see cref="ClaimTypes.NameIdentifier"/> when inbound mapping is on, and leaves it as
/// <c>sub</c> when it is not — so lookups have to accept both.
/// </summary>
internal static class ClaimsPrincipalExtensions
{
    private static readonly string[] UserIdClaimTypes =
    [
        ClaimTypes.NameIdentifier,
        "sub",
        "nameid",
    ];

    public static string? FindUserId(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        foreach (string type in UserIdClaimTypes)
        {
            string? value = principal.FindFirstValue(type);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
