using Microsoft.AspNetCore.Components;

namespace ReelSoon.Components.Shared;

public partial class RedirectToLogin
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IAuthProviderService Auth { get; set; } = default!;

    protected override void OnInitialized()
    {
        string returnUrl = Uri.EscapeDataString(
            Navigation.ToBaseRelativePath(Navigation.Uri) is { Length: > 0 } relative
                ? $"/{relative}"
                : "/");

        string loginUrl = Auth.Provider switch
        {
            AuthProvider.Keycloak => $"auth/login?returnUrl={returnUrl}",
            AuthProvider.Identity => $"Identity/Account/Login?ReturnUrl={returnUrl}",
            _ => "/",
        };

        Navigation.NavigateTo(loginUrl, forceLoad: true);
    }
}
