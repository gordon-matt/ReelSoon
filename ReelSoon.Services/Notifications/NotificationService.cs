namespace ReelSoon.Services.Notifications;

public sealed class NotificationService(
    IUserContextService userContext,
    IRepository<WatchNotification> notifications,
    TimeProvider time) : INotificationService
{
    public async Task<Result<IReadOnlyList<WatchNotificationDto>>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        if (ResolveOwner() is not string ownerUserId)
        {
            return Result<IReadOnlyList<WatchNotificationDto>>.Unauthorized();
        }

        var page = await notifications.FindAsync(new SearchOptions<WatchNotification>
        {
            Query = notice => notice.OwnerUserId == ownerUserId,
            Include = query => query.Include(notice => notice.WatchItem),
            OrderBy = query => query.OrderByDescending(notice => notice.CreatedAt),
            CancellationToken = cancellationToken,
        });

        return Result<IReadOnlyList<WatchNotificationDto>>.Success(page.Select(ToDto).ToList());
    }

    public async Task<Result<int>> UnreadCountAsync(CancellationToken cancellationToken = default)
    {
        if (ResolveOwner() is not string ownerUserId)
        {
            return Result<int>.Unauthorized();
        }

        int count = await notifications.CountAsync(
            notice => notice.OwnerUserId == ownerUserId && notice.ReadAt == null,
            new ContextOptions { CancellationToken = cancellationToken });

        return Result<int>.Success(count);
    }

    public async Task<Result<WatchNotificationDto>> MarkReadAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var found = await FindOwnedAsync(id, cancellationToken);
        if (!found.IsSuccessStatus())
        {
            return found.To<WatchNotificationDto>();
        }

        var notice = found.Value;
        if (notice.ReadAt is null)
        {
            notice.ReadAt = time.GetUtcNow().UtcDateTime;
            await notifications.UpdateAsync(notice);
        }

        return Result<WatchNotificationDto>.Success(ToDto(notice));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var found = await FindOwnedAsync(id, cancellationToken);
        if (!found.IsSuccessStatus())
        {
            return found.ToVoid();
        }

        await notifications.DeleteAsync(found.Value);
        return Result.Success();
    }

    private async Task<Result<WatchNotification>> FindOwnedAsync(int id, CancellationToken cancellationToken)
    {
        if (ResolveOwner() is not string ownerUserId)
        {
            return Result<WatchNotification>.Unauthorized();
        }

        var notice = await notifications.FindOneAsync(new SearchOptions<WatchNotification>
        {
            Query = candidate => candidate.Id == id && candidate.OwnerUserId == ownerUserId,
            Include = query => query.Include(candidate => candidate.WatchItem),
            CancellationToken = cancellationToken,
        });

        return notice is null
            ? Result<WatchNotification>.NotFound("That notification is already gone.")
            : Result<WatchNotification>.Success(notice);
    }

    private string? ResolveOwner() => userContext.GetCurrentUserId();

    private static WatchNotificationDto ToDto(WatchNotification notice) => new(
        notice.Id,
        notice.WatchItemId,
        notice.WatchItem?.Kind ?? MediaKind.Movie,
        notice.Title,
        notice.Message,
        notice.CreatedAt,
        notice.ReadAt);
}