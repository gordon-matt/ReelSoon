using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ReelSoon.Data;

/// <summary>
/// Provider-agnostic context. Each <c>ReelSoon.Data.{Provider}</c> project subclasses this and
/// owns its own migration history, because EF bakes provider-specific column types into
/// migrations.
/// </summary>
public abstract class ApplicationDbContextBase
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    protected ApplicationDbContextBase(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<WatchItem> WatchItems => Set<WatchItem>();

    public DbSet<WatchNotification> WatchNotifications => Set<WatchNotification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema(Constants.Schemas.App);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContextBase).Assembly);

        ConfigureIdentityTables(builder);
    }

    /// <summary>
    /// Renames the Identity tables into the <c>app</c> schema so a deployment doesn't end up with
    /// <c>AspNetUsers</c> sitting next to <c>app.WatchItems</c>.
    /// </summary>
    protected static void ConfigureIdentityTables(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>().ToTable("Users", Constants.Schemas.App);
        builder.Entity<ApplicationRole>().ToTable("Roles", Constants.Schemas.App);
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", Constants.Schemas.App);
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", Constants.Schemas.App);
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", Constants.Schemas.App);
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", Constants.Schemas.App);
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", Constants.Schemas.App);
    }
}
