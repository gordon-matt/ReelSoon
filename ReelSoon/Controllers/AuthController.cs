using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ReelSoon.Controllers;

/// <summary>
/// Sign-in for Keycloak, and sign-out for both Keycloak and ASP.NET Identity. Identity's login
/// page is a Razor Page; this controller only challenges the IdP.
/// </summary>
[Route("auth")]
[AllowAnonymous]
public sealed class AuthController(IAuthProviderService authProvider, IServiceProvider services) : Controller
{
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        if (authProvider.Provider != AuthProvider.Keycloak)
        {
            return Redirect("/");
        }

        return Challenge(
            new AuthenticationProperties { RedirectUri = SafeReturnUrl(returnUrl) },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        if (authProvider.Provider == AuthProvider.Keycloak)
        {
            return SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        if (authProvider.Provider == AuthProvider.Identity)
        {
            var signInManager = services.GetRequiredService<SignInManager<ApplicationUser>>();
            await signInManager.SignOutAsync();
        }

        return Redirect("/");
    }

    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
}
