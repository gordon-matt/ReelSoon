namespace ReelSoon;

public static class Constants
{
    /// <summary>Browser <c>localStorage</c> key for the light/dark theme.</summary>
    public const string ClientThemeLocalStorageKey = "reelsoon.theme";

    public static class MaxLengths
    {
        public const int Title = 300;
        public const int Notes = 4000;
        public const int Url = 2000;
        public const int ExternalId = 64;
        public const int Message = 500;
        public const int UserId = 450;
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