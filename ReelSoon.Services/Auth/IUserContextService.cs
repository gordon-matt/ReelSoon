namespace ReelSoon.Services.Auth;

/// <summary>Who is making the current request, independent of which auth provider is active.</summary>
public interface IUserContextService
{
    string? GetCurrentUserId();

    string? GetCurrentUserName();

    bool IsAuthenticated();

    bool IsAdministrator();

    IReadOnlyList<string> GetRoleNames();
}
