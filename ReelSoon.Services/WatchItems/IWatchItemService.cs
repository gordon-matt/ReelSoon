namespace ReelSoon.Services.WatchItems;

public interface IWatchItemService
{
    Task<Result<IReadOnlyList<WatchItemDto>>> ListAsync(
        MediaKind kind,
        string? search,
        CancellationToken cancellationToken = default);

    Task<Result<WatchListCounts>> GetCountsAsync(CancellationToken cancellationToken = default);

    Task<Result<WatchItemDto>> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<WatchItemDto>> CreateAsync(
        MediaKind kind,
        WatchItemForm form,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a title found online, storing its poster, description, link, and release date.
    /// A title already saved for that catalog id is returned instead of being inserted again.
    /// </summary>
    Task<Result<CatalogAdd>> AddFromCatalogAsync(
        MediaKind kind,
        CatalogHit hit,
        CancellationToken cancellationToken = default);

    Task<Result<WatchItemDto>> UpdateAsync(
        int id,
        WatchItemForm form,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
