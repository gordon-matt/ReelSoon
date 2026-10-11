using ReelSoon.Services.Notifications;

namespace ReelSoon.Services.Catalog;

public sealed class CatalogRefreshService(
    ILogger<CatalogRefreshService> logger,
    IRepository<WatchItem> items,
    IRepository<WatchNotification> notifications,
    ICatalogService catalog,
    IReleaseNotificationService releases,
    TimeProvider time) : ICatalogRefreshService
{
    public async Task<int> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var linked = await items.FindAsync(new SearchOptions<WatchItem>
        {
            Query = item => item.ExternalId != null && item.CatalogSource != null,
            CancellationToken = cancellationToken,
        });

        int updated = 0;

        foreach (var item in linked)
        {
            if (item.CatalogSource is not CatalogSource source || string.IsNullOrWhiteSpace(item.ExternalId))
            {
                continue;
            }

            try
            {
                var fresh = await catalog.GetAsync(item.Kind, source, item.ExternalId, cancellationToken);
                if (!fresh.IsSuccess)
                {
                    logger.LogInformation(
                        "Skipped catalog refresh for {Title}: {Reason}",
                        item.Title,
                        fresh.GetDisplayMessage());
                    continue;
                }

                if (await ApplyAsync(item, fresh.Value, cancellationToken))
                {
                    updated++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Catalog refresh failed for watch item {Id}", item.Id);
            }
        }

        await releases.NotifyDueAsync(cancellationToken);

        if (updated > 0)
        {
            logger.LogInformation("Refreshed catalog details for {Count} title(s)", updated);
        }

        return updated;
    }

    private async Task<bool> ApplyAsync(WatchItem item, CatalogHit hit, CancellationToken cancellationToken)
    {
        bool changed = false;
        bool dateChanged = false;

        if (hit.Title.Length > 0 && item.Title != hit.Title)
        {
            item.Title = hit.Title;
            changed = true;
        }

        if (hit.Overview is not null && item.Overview != hit.Overview)
        {
            item.Overview = hit.Overview;
            changed = true;
        }

        if (hit.ImageUrl is not null && item.ImageUrl != hit.ImageUrl)
        {
            item.ImageUrl = hit.ImageUrl;
            changed = true;
        }

        if (hit.LinkUrl is not null && item.LinkUrl != hit.LinkUrl)
        {
            item.LinkUrl = hit.LinkUrl;
            changed = true;
        }

        // A missing remote date must not wipe a date the user typed in.
        if (hit.ReleaseDate is not null && item.ReleaseDate != hit.ReleaseDate)
        {
            item.ReleaseDate = hit.ReleaseDate;
            item.ReleaseNotifiedAt = null;
            changed = true;
            dateChanged = true;
        }

        if (!changed)
        {
            return false;
        }

        if (dateChanged)
        {
            await notifications.DeleteAsync(
                notice => notice.WatchItemId == item.Id,
                new ContextOptions { CancellationToken = cancellationToken });
        }

        item.UpdatedAt = time.GetUtcNow().UtcDateTime;
        await items.UpdateAsync(item);
        return true;
    }
}
