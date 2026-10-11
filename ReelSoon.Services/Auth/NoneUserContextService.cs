namespace ReelSoon.Services.Auth;

/// <summary>
/// The whole-machine-is-yours context used when <c>Authentication:Provider</c> is <c>None</c>.
/// Always the synthetic administrator.
/// </summary>
public sealed class NoneUserContextService : IUserContextService
{
    private static readonly IReadOnlyList<string> Roles =
        [Constants.Roles.Administrator, Constants.Roles.User];

    public string? GetCurrentUserId() => Constants.DefaultUserId;

    public string? GetCurrentUserName() => Constants.DefaultUserName;

    public bool IsAuthenticated() => true;

    public bool IsAdministrator() => true;

    public IReadOnlyList<string> GetRoleNames() => Roles;
}
