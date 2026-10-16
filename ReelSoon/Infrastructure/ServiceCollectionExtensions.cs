using Hangfire;
using Heroicsoft.NetForge.Abstractions.Data;
using ReelSoon.Data.MySql;
using ReelSoon.Data.Npgsql;
using ReelSoon.Data.Sql;
using ReelSoon.Data.Sqlite;

namespace ReelSoon.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Reads <c>Database:Provider</c> and delegates to the matching provider project. Failing
    /// loudly on an unknown value beats silently starting on SQLite when the operator meant
    /// Postgres and mistyped it.
    /// </summary>
    public static IServiceCollection AddReelSoonDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string provider = ReelSoonConfiguration.ResolveDatabaseProvider(configuration);

        return provider switch
        {
            DatabaseProviders.Sqlite => services.AddReelSoonSqlite(configuration),
            DatabaseProviders.SqlServer => services.AddReelSoonSqlServer(configuration),
            DatabaseProviders.Npgsql => services.AddReelSoonNpgsql(configuration),
            DatabaseProviders.MySql => services.AddReelSoonMySql(configuration),
            _ => throw new InvalidOperationException(
                $"Unknown Database:Provider '{provider}'. Valid values: Sqlite, SqlServer, Npgsql, MySql."),
        };
    }

    public static IServiceCollection AddReelSoonRepositories(this IServiceCollection services)
    {
        services.AddEntityFrameworkRepository();
        return services;
    }

    /// <summary>
    /// Stores Hangfire jobs in the same database family as the app. SQLite gets its own file.
    /// </summary>
    public static IServiceCollection AddReelSoonHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string provider = ReelSoonConfiguration.ResolveDatabaseProvider(configuration);
        string? hangfireExplicit = configuration.GetConnectionString("Hangfire");
        string? connectionString = string.IsNullOrWhiteSpace(hangfireExplicit)
            ? configuration.GetConnectionString("DefaultConnection")
            : hangfireExplicit;

        switch (provider)
        {
            case DatabaseProviders.Sqlite:
                services.AddReelSoonSqliteHangfire(configuration["Hangfire:SqlitePath"]);
                break;

            case DatabaseProviders.SqlServer:
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Hangfire requires a connection string when using SqlServer.");
                }

                services.AddReelSoonSqlServerHangfire(connectionString);
                break;

            case DatabaseProviders.Npgsql:
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Hangfire requires a connection string when using Npgsql.");
                }

                services.AddReelSoonNpgsqlHangfire(connectionString);
                break;

            case DatabaseProviders.MySql:
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Hangfire requires a connection string when using MySql.");
                }

                services.AddReelSoonMySqlHangfire(connectionString);
                break;

            default:
                throw new InvalidOperationException($"Unknown Database:Provider '{provider}'.");
        }

        services.AddHangfireServer(options =>
        {
            options.ServerName = "main";
            options.WorkerCount = 2;
        });

        return services;
    }

    /// <summary>
    /// Wires up whichever authentication provider is configured and returns which one it was, so
    /// <c>Program.cs</c> can decide whether Identity's Razor Pages need mapping.
    /// </summary>
    public static AuthProvider AddReelSoonAuthentication(
        this IServiceCollection services,
        IConfiguration configuration) =>
        // Identity, Keycloak, and None — including IUserContextService — come from NetForge.
        // This host does not register an IUserInfoService.
        services.AddNetForgeAuthentication<ApplicationUser, ApplicationRole, ApplicationDbContextBase>(
            configuration);
}