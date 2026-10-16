using Hangfire;
using ReelSoon.Services.Jobs;

namespace ReelSoon.Infrastructure;

/// <summary>Registers recurring jobs shared by every host.</summary>
public static class HangfireRecurringJobs
{
    public static void Register()
    {
        RecurringJob.AddOrUpdate<ReleaseCheckJob>(
            "release-check",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(5),
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        RecurringJob.AddOrUpdate<CatalogRefreshJob>(
            "catalog-refresh",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(4),
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}