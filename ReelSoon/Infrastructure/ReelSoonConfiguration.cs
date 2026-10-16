using Heroicsoft.NetForge.Abstractions.Data;

namespace ReelSoon.Infrastructure;

public static class ReelSoonConfiguration
{
    public static string ResolveDatabaseProvider(IConfiguration configuration) =>
        DatabaseProviders.Resolve(
            configuration[DatabaseProviders.ConfigurationKey],
            DatabaseProviders.Sqlite,
            [.. DatabaseProviders.All]);
}