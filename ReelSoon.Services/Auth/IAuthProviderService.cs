namespace ReelSoon.Services.Auth;

public enum AuthProvider
{
    Identity = 0,
    Keycloak = 1,
    None = 2,
}

/// <summary>
/// Exposes the configured auth mode to the UI so it can hide what doesn't apply — no sign-in
/// menu in <c>None</c> mode, no local password fields under Keycloak.
/// </summary>
public interface IAuthProviderService
{
    AuthProvider Provider { get; }

    string? KeycloakAuthority { get; }
}

public sealed class AuthProviderService(AuthProvider provider, string? keycloakAuthority) : IAuthProviderService
{
    public AuthProvider Provider { get; } = provider;

    public string? KeycloakAuthority { get; } = keycloakAuthority;
}
