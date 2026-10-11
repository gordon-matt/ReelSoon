using ReelSoon.Services.Notifications;
using ReelSoon.Services.WatchItems;

namespace ReelSoon.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddReelSoonServices(this IServiceCollection services)
    {
        services.AddScoped<IWatchItemService, WatchItemService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReleaseNotificationService, ReleaseNotificationService>();

        return services;
    }
}
