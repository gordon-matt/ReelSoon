namespace ReelSoon.Infrastructure;

/// <summary>
/// Circuit-scoped signals so a save on the list page can refresh the sidebar counts and the
/// notification badge without those components polling each other.
/// </summary>
public sealed class AppEvents
{
    public event Action? ListsChanged;

    public event Action? NotificationsChanged;

    public void RaiseListsChanged() => ListsChanged?.Invoke();

    public void RaiseNotificationsChanged() => NotificationsChanged?.Invoke();
}
