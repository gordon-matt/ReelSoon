namespace ReelSoon.Services.Notifications;

/// <summary>
/// Creates a notification for every title whose release date has arrived and that has not
/// already been announced for that date.
/// </summary>
public interface IReleaseNotificationService
{
    Task<int> NotifyDueAsync(CancellationToken cancellationToken = default);
}
