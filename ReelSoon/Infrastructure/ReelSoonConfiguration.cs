namespace ReelSoon.Infrastructure;

public static class ReelSoonConfiguration
{
    public static string ResolveDatabaseProvider(IConfiguration configuration) =>
        configuration["Database:Provider"] is { Length: > 0 } provider
            ? provider.Trim()
            : Constants.DatabaseProviders.Sqlite;
}
