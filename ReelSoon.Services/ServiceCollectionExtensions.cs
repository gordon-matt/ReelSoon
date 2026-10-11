using ReelSoon.Services.Catalog;
using ReelSoon.Services.Jobs;
using ReelSoon.Services.Notifications;
using ReelSoon.Services.WatchItems;

namespace ReelSoon.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReelSoonServices(this IServiceCollection services)
    {
        services.AddHttpClient<TmdbClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddHttpClient<IgdbClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.igdb.com/v4/");
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddHttpClient(nameof(IgdbSession), client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddSingleton<IgdbSession>();

        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICatalogRefreshService, CatalogRefreshService>();
        services.AddScoped<IWatchItemService, WatchItemService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReleaseNotificationService, ReleaseNotificationService>();
        services.AddScoped<ReleaseCheckJob>();
        services.AddScoped<CatalogRefreshJob>();

        return services;
    }
}
