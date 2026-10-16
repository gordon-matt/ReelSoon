using ReelSoon.Services.Catalog;

namespace ReelSoon.Services.Jobs;

/// <summary>Hangfire entry point that refreshes posters, descriptions, and release dates once a day.</summary>
public sealed class CatalogRefreshJob(ICatalogRefreshService refresh, ILogger<CatalogRefreshJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await refresh.RefreshAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Catalog refresh failed");
            throw;
        }
    }
}