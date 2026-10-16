namespace ReelSoon.Services.Notifications;

public sealed class ReleaseNotificationService(
    ILogger<ReleaseNotificationService> logger,
    IRepository<WatchItem> items,
    IRepository<WatchNotification> notifications,
    TimeProvider time) : IReleaseNotificationService
{
    public async Task<int> NotifyDueAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);

        var due = await items.FindAsync(new SearchOptions<WatchItem>
        {
            Query = item => item.ReleaseDate != null
                && item.ReleaseDate <= today
                && item.ReleaseNotifiedAt == null,
            CancellationToken = cancellationToken,
        });

        if (due.Count == 0)
        {
            return 0;
        }

        var now = time.GetUtcNow().UtcDateTime;
        int created = 0;

        foreach (var item in due)
        {
            if (item.ReleaseDate is not DateOnly releaseDate)
            {
                continue;
            }

            try
            {
                await notifications.InsertAsync(new WatchNotification
                {
                    OwnerUserId = item.OwnerUserId,
                    WatchItemId = item.Id,
                    Title = item.Title,
                    Message = Describe(item.Kind, releaseDate, today),
                    CreatedAt = now,
                });
                created++;
            }
            catch (DbUpdateException ex)
            {
                // A concurrent check already inserted the one allowed notice for this title.
                logger.LogDebug(ex, "Release notice for watch item {Id} already exists", item.Id);
            }

            item.ReleaseNotifiedAt = now;
            await items.UpdateAsync(item);
        }

        if (created > 0)
        {
            logger.LogInformation("Created {Count} release notification(s)", created);
        }

        return created;
    }

    private static string Describe(MediaKind kind, DateOnly releaseDate, DateOnly today)
    {
        string label = kind.Singular().ToLowerInvariant();

        return releaseDate == today
            ? $"This {label} is out today."
            : $"This {label} was released on {ReleaseText.Long(releaseDate)}.";
    }
}