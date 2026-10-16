namespace ReelSoon.Services.Notifications;

public interface INotificationService
{
    Task<Result<IReadOnlyList<WatchNotificationDto>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<int>> UnreadCountAsync(CancellationToken cancellationToken = default);

    Task<Result<WatchNotificationDto>> MarkReadAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}