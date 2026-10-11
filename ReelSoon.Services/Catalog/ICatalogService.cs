namespace ReelSoon.Services.Catalog;

public interface ICatalogService
{
    Task<Result<IReadOnlyList<CatalogHit>>> SearchAsync(
        MediaKind kind,
        string query,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the current poster, description, and release date for one catalog id.</summary>
    Task<Result<CatalogHit>> GetAsync(
        MediaKind kind,
        CatalogSource source,
        string externalId,
        CancellationToken cancellationToken = default);
}

public interface ICatalogRefreshService
{
    /// <summary>
    /// Rewrites poster, description, link, and release date for every title that was added from
    /// a catalog. Returns how many titles changed.
    /// </summary>
    Task<int> RefreshAsync(CancellationToken cancellationToken = default);
}
