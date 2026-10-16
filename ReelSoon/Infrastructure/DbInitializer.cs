using Microsoft.AspNetCore.Identity;

namespace ReelSoon.Infrastructure;

/// <summary>
/// Brings the database up to date at startup and seeds the minimum an instance needs to be usable.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        AuthProvider authProvider,
        IConfiguration configuration,
        ILogger logger)
    {
        var context = services.GetRequiredService<ApplicationDbContextBase>();

        if (context.Database.IsRelational())
        {
            logger.LogInformation("Applying database migrations");
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        switch (authProvider)
        {
            case AuthProvider.Identity:
                await SeedRolesAsync(services, logger);
                await SeedAdministratorAsync(services, configuration, logger);
                break;

            case AuthProvider.Keycloak:
                await SeedRolesAsync(services, logger);
                break;

            case AuthProvider.None:
                break;
        }
    }

    private static async Task SeedRolesAsync(IServiceProvider services, ILogger logger)
    {
        var roleManager = services.GetService<RoleManager<ApplicationRole>>();

        if (roleManager is null)
        {
            return;
        }

        var roles = new (string Name, string Description)[]
        {
            (WellKnownRoles.Administrator, "Manages the instance"),
            (WellKnownRoles.User, "Keeps their own watch lists"),
        };

        foreach (var (name, description) in roles)
        {
            if (await roleManager.RoleExistsAsync(name))
            {
                continue;
            }

            await roleManager.CreateAsync(new ApplicationRole { Name = name, Description = description });
            logger.LogInformation("Created role '{Role}'", name);
        }
    }

    private static async Task SeedAdministratorAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        var userManager = services.GetService<UserManager<ApplicationUser>>();

        if (userManager is null)
        {
            return;
        }

        var existing = await userManager.GetUsersInRoleAsync(WellKnownRoles.Administrator);

        if (existing.Count > 0)
        {
            return;
        }

        string? password = configuration["Seed:Admin:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation(
                "No administrator exists and Seed:Admin:Password is not set. Register the first account from the sign-in page.");
            return;
        }

        string email = configuration["Seed:Admin:Email"] ?? "admin@reelsoon.local";
        string userName = configuration["Seed:Admin:UserName"] is { Length: > 0 } configuredName
            ? configuredName
            : email;

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            DisplayName = "Administrator",
        };

        var created = await userManager.CreateAsync(user, password);

        if (!created.Succeeded)
        {
            logger.LogError(
                "Could not create the seed administrator: {Errors}",
                string.Join("; ", created.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, WellKnownRoles.Administrator);
        await userManager.AddToRoleAsync(user, WellKnownRoles.User);

        logger.LogWarning(
            "Seed administrator '{UserName}' was created from configuration. Change its password.",
            userName);
    }
}