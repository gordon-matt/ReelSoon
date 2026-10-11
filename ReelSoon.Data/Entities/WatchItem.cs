namespace ReelSoon.Data.Entities;

/// <summary>
/// Something the user wants to know about when it comes out: a movie, a series, or a game.
/// </summary>
public class WatchItem : BaseEntity<int>
{
    public MediaKind Kind { get; set; }

    public required string Title { get; set; }

    public string? Notes { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? LinkUrl { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>Plot or description copied from the catalog. Separate from the user's own notes.</summary>
    public string? Overview { get; set; }

    /// <summary>Set when the title was added from an online search. Manual titles leave this empty.</summary>
    public CatalogSource? CatalogSource { get; set; }

    /// <summary>TMDB or IGDB id, used to refresh the poster, description, and release date.</summary>
    public string? ExternalId { get; set; }

    /// <summary>Identity user id, Keycloak <c>sub</c>, or the synthetic id in <c>None</c> mode.</summary>
    public required string OwnerUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// When a release notification was created for the current <see cref="ReleaseDate"/>.
    /// Cleared if that date changes, so a postponed title can notify again.
    /// </summary>
    public DateTime? ReleaseNotifiedAt { get; set; }

    public ICollection<WatchNotification> Notifications { get; set; } = [];
}

public class WatchItemMap : IEntityTypeConfiguration<WatchItem>
{
    public void Configure(EntityTypeBuilder<WatchItem> builder)
    {
        builder.ToTable("WatchItems", Constants.Schemas.App);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Title).IsRequired().HasMaxLength(Constants.MaxLengths.Title).IsUnicode(true);
        builder.Property(m => m.Notes).HasMaxLength(Constants.MaxLengths.Notes).IsUnicode(true);
        builder.Property(m => m.LinkUrl).HasMaxLength(Constants.MaxLengths.Url).IsUnicode(false);
        builder.Property(m => m.ImageUrl).HasMaxLength(Constants.MaxLengths.Url).IsUnicode(false);
        builder.Property(m => m.Overview).HasMaxLength(Constants.MaxLengths.Notes).IsUnicode(true);
        builder.Property(m => m.ExternalId).HasMaxLength(Constants.MaxLengths.ExternalId).IsUnicode(false);
        builder.Property(m => m.OwnerUserId).IsRequired().HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(false);

        builder.HasIndex(m => new { m.OwnerUserId, m.Kind, m.Title });
        builder.HasIndex(m => new { m.OwnerUserId, m.Kind, m.CatalogSource, m.ExternalId });
        builder.HasIndex(m => new { m.ReleaseDate, m.ReleaseNotifiedAt });
    }
}
