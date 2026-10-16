namespace ReelSoon.Infrastructure;

/// <summary>
/// Circuit-scoped signal so a save on the list page can refresh the notification badge and the
/// open list without those components polling each other. Sidebar counts go through SidebarRefresh.
/// </summary>
public sealed class AppEvents
{
    public event Action? NotificationsChanged;

    public void RaiseNotificationsChanged() => NotificationsChanged?.Invoke();
}