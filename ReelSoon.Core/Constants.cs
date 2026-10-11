namespace ReelSoon;

public static class Constants
{
    /// <summary>Browser <c>localStorage</c> key for the light/dark theme.</summary>
    public const string ClientThemeLocalStorageKey = "reelsoon.theme";

    /// <summary>Synthetic user id used by the "None" authentication mode.</summary>
    public const string DefaultUserId = "_default";

    /// <summary>Display name used for the synthetic user when no real auth provider is active.</summary>
    public const string DefaultUserName = "Default User";

    public static class AuthProviders
    {
        public const string Identity = "Identity";
        public const string Keycloak = "Keycloak";
        public const string None = "None";
    }

    public static class DatabaseProviders
    {
        public const string Sqlite = "Sqlite";
        public const string SqlServer = "SqlServer";
        public const string Npgsql = "Npgsql";
        public const string MySql = "MySql";
    }

    public static class MaxLengths
    {
        public const int Title = 300;
        public const int Notes = 4000;
        public const int Url = 2000;
        public const int ExternalId = 64;
        public const int Message = 500;
        public const int UserId = 450;
    }

    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string User = "User";
    }

    public static class Schemas
    {
        public const string App = "app";
    }

    public static class ServerSettingDefaults
    {
        public const string SiteName = "ReelSoon";
    }
}
