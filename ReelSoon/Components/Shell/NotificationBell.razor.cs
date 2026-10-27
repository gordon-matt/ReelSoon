using Microsoft.AspNetCore.Components;

namespace ReelSoon.Components.Shell;

public partial class NotificationBell : IDisposable
{
    private readonly CancellationTokenSource pollCts = new();
    private bool open;
    private int unreadCount;

    [Inject]
    private INotificationService Notifications { get; set; } = default!;

    [Inject]
    private AppEvents Events { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Events.NotificationsChanged += OnNotificationsChanged;
        await RefreshUnreadAsync();
        _ = PollUnreadAsync(pollCts.Token);
    }

    private void Toggle() => open = !open;

    private void Close() => open = false;

    private void OnNotificationsChanged() => InvokeAsync(RefreshUnreadAsync);

    private async Task RefreshUnreadAsync()
    {
        var result = await Notifications.UnreadCountAsync();

        if (!result.IsSuccess)
        {
            return;
        }

        bool newlyReleased = result.Value > unreadCount;
        unreadCount = result.Value;
        StateHasChanged();

        // The background checker has no circuit, so a rising count is how an open list learns
        // that a title just flipped to released.
        if (newlyReleased)
        {
            Events.RaiseNotificationsChanged();
        }
    }

    private async Task PollUnreadAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await RefreshUnreadAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public void Dispose()
    {
        Events.NotificationsChanged -= OnNotificationsChanged;
        pollCts.Cancel();
        pollCts.Dispose();
    }
}