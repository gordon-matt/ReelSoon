using Microsoft.Extensions.Options;

namespace ReelSoon.Infrastructure;

/// <summary>
/// Periodically asks <see cref="IReleaseNotificationService"/> to announce titles that are out.
/// The same service also runs after a save, so adding something already released does not wait
/// for the next tick.
/// </summary>
public sealed class ReleaseCheckWorker(
    IServiceScopeFactory scopes,
    IOptions<ReleaseCheckOptions> options,
    ILogger<ReleaseCheckWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CheckAsync(stoppingToken);

        var interval = TimeSpan.FromSeconds(Math.Max(15, options.Value.CheckIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CheckAsync(stoppingToken);
        }
    }

    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var releases = scope.ServiceProvider.GetRequiredService<IReleaseNotificationService>();
            await releases.NotifyDueAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Release check failed");
        }
    }
}
