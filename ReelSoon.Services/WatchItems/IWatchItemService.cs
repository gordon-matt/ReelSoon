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

    Task<Result<WatchItemDto>> UpdateAsync(
        int id,
        WatchItemForm form,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
