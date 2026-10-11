using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ReelSoon.Infrastructure;

/// <summary>
/// Authenticates every request as a single synthetic administrator. Useful for a machine that
/// only one person can reach. It must not be selected for a server anyone else can open.
/// </summary>
public sealed class NoneAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "None";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Constants.DefaultUserId),
                new Claim(ClaimTypes.Name, Constants.DefaultUserName),
                new Claim(ClaimTypes.Role, Constants.Roles.Administrator),
                new Claim(ClaimTypes.Role, Constants.Roles.User),
            ],
            authenticationType: SchemeName,
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
