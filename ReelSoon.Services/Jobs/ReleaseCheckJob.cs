using ReelSoon.Services.Notifications;

namespace ReelSoon.Services.Jobs;

/// <summary>Hangfire entry point for the release-notification check.</summary>
public sealed class ReleaseCheckJob(IReleaseNotificationService releases, ILogger<ReleaseCheckJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await releases.NotifyDueAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Release check failed");
            throw;
        }
    }
}
