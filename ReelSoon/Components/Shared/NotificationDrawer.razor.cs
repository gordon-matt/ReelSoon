using Microsoft.AspNetCore.Components;

namespace ReelSoon.Components.Shared;

public partial class NotificationDrawer : IDisposable
{
    [Inject]
    private INotificationService Notifications { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ToastService Toasts { get; set; } = default!;

    [Inject]
    private AppEvents Events { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    [Parameter]
    public EventCallback OnClose { get; set; }

    private bool loading = true;
    private string? loadError;
    private List<WatchNotificationDto> items = [];

    protected override async Task OnInitializedAsync()
    {
        Events.NotificationsChanged += OnChanged;
        await LoadAsync();
    }

    private void OnChanged() => InvokeAsync(LoadAsync);

    private async Task LoadAsync()
    {
        var listed = await Notifications.ListAsync();
        loading = false;

        if (!listed.IsSuccess)
        {
            loadError = listed.GetDisplayMessage();
            items = [];
            return;
        }

        loadError = null;
        items = [.. listed.Value];
    }

    private async Task ViewAsync(WatchNotificationDto notice)
    {
        if (notice.IsUnread)
        {
            var read = await Notifications.MarkReadAsync(notice.Id);
            if (!read.IsSuccess)
            {
                Toasts.Error(read.GetDisplayMessage());
                return;
            }

            Events.RaiseNotificationsChanged();
        }

        await OnClose.InvokeAsync();
        Navigation.NavigateTo($"{notice.Kind.Route()}?id={notice.WatchItemId}");
    }

    private async Task DeleteAsync(WatchNotificationDto notice)
    {
        var deleted = await Notifications.DeleteAsync(notice.Id);
        if (!deleted.IsSuccess)
        {
            Toasts.Error(deleted.GetDisplayMessage());
            return;
        }

        items.RemoveAll(item => item.Id == notice.Id);
        Events.RaiseNotificationsChanged();
    }

    private string When(DateTime createdUtc)
    {
        var now = Time.GetUtcNow().UtcDateTime;
        var delta = now - createdUtc;

        if (delta.TotalMinutes < 1)
        {
            return "Just now";
        }

        if (delta.TotalHours < 1)
        {
            return $"{(int)delta.TotalMinutes}m ago";
        }

        if (delta.TotalDays < 1)
        {
            return $"{(int)delta.TotalHours}h ago";
        }

        if (delta.TotalDays < 7)
        {
            return $"{(int)delta.TotalDays}d ago";
        }

        return createdUtc.ToLocalTime().ToString("d MMM yyyy", System.Globalization.CultureInfo.CurrentCulture);
    }

    public void Dispose() => Events.NotificationsChanged -= OnChanged;
}
