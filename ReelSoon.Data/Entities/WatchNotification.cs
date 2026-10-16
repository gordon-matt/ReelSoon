namespace ReelSoon.Data.Entities;

/// <summary>A per-user notice, currently raised when a watched title's release date arrives.</summary>
public class WatchNotification : BaseEntity<int>
{
    public required string OwnerUserId { get; set; }

    public int WatchItemId { get; set; }

    public required string Title { get; set; }

    public required string Message { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public WatchItem WatchItem { get; set; } = null!;
}

public class WatchNotificationMap : IEntityTypeConfiguration<WatchNotification>
{
    public void Configure(EntityTypeBuilder<WatchNotification> builder)
    {
        builder.ToTable("WatchNotifications", Constants.Schemas.App);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.OwnerUserId).IsRequired().HasMaxLength(Constants.MaxLengths.UserId).IsUnicode(false);
        builder.Property(m => m.Title).IsRequired().HasMaxLength(Constants.MaxLengths.Title).IsUnicode(true);
        builder.Property(m => m.Message).IsRequired().HasMaxLength(Constants.MaxLengths.Message).IsUnicode(true);

        // One release notice per title. A changed release date deletes the old row first.
        builder.HasIndex(m => m.WatchItemId).IsUnique();
        builder.HasIndex(m => new { m.OwnerUserId, m.ReadAt, m.CreatedAt });

        builder.HasOne(m => m.WatchItem)
            .WithMany(m => m.Notifications)
            .HasForeignKey(m => m.WatchItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}