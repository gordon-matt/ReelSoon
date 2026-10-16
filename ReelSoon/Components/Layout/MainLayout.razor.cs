using Heroicsoft.NetForge.Abstractions.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace ReelSoon.Components.Layout;

public partial class MainLayout : IDisposable
{
    private const string BADGE_TONE = "nf-badge-muted";

    [Inject]
    private IOptions<ForgeShellOptions> ShellOptions { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IWatchItemService WatchItems { get; set; } = default!;

    [Inject]
    private INotificationService Notifications { get; set; } = default!;

    [Inject]
    private AppEvents Events { get; set; } = default!;

    [Inject]
    private SidebarRefresh Sidebar { get; set; } = default!;

    private readonly CancellationTokenSource pollCts = new();
    private ForgeShellOptions shell = new();
    private bool notificationsOpen;
    private WatchListCounts? counts;
    private int unreadCount;

    /// <summary>Pinned under a divider. Both dashboards live outside the Blazor router.</summary>
    private static readonly ForgeNavItem[] AdminLinks =
    [
        new() { Title = "Jobs", Href = "/hangfire", Icon = "jobs", External = true },
        new() { Title = "Logs", Href = "/logvue", Icon = "logs", External = true },
    ];

    /// <summary>The watch lists with their counts.</summary>
    private IReadOnlyList<ForgeNavSection> Sections =>
    [
        new ForgeNavSection
        {
            Heading = "Watch lists",
            Items =
            [
                .. MediaKinds.All.Select(kind => new ForgeNavItem
                {
                    Title = kind.Label(),
                    Href = kind.Route(),
                    Icon = kind.Icon(),
                    Badge = counts?.For(kind).ToString(),
                    BadgeCssClass = BADGE_TONE,
                }),
            ],
        },
    ];

    protected override async Task OnInitializedAsync()
    {
        shell = ShellOptions.Value;
        Sidebar.Changed += OnSidebarChanged;
        Events.NotificationsChanged += OnNotificationsChanged;

        await RefreshCountsAsync();
        await RefreshUnreadAsync();

        _ = PollUnreadAsync(pollCts.Token);
    }

    private void ToggleNotifications() => notificationsOpen = !notificationsOpen;

    private void CloseNotifications() => notificationsOpen = false;

    private void Search(string term) =>
        Navigation.NavigateTo($"{ListRoute()}?q={Uri.EscapeDataString(term)}");

    private void OnSidebarChanged() => InvokeAsync(RefreshCountsAsync);

    private void OnNotificationsChanged() => InvokeAsync(RefreshUnreadAsync);

    private string ListRoute()
    {
        string path = Navigation.ToBaseRelativePath(Navigation.Uri).Split('?', '#')[0];
        return MediaKinds.TryFromRoute(path, out var kind) ? kind.Route() : MediaKind.Movie.Route();
    }

    private async Task RefreshCountsAsync()
    {
        var result = await WatchItems.GetCountsAsync();
        if (result.IsSuccess)
        {
            counts = result.Value;
            StateHasChanged();
        }
    }

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
        Sidebar.Changed -= OnSidebarChanged;
        Events.NotificationsChanged -= OnNotificationsChanged;
        pollCts.Cancel();
        pollCts.Dispose();
    }
}