using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace ReelSoon.Components.Layout;

public partial class MainLayout : IAsyncDisposable
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private IJSRuntime Js { get; set; } = default!;

    [Inject]
    private AuthenticationStateProvider AuthState { get; set; } = default!;

    [Inject]
    private IAuthProviderService Auth { get; set; } = default!;

    [Inject]
    private IWatchItemService WatchItems { get; set; } = default!;

    [Inject]
    private INotificationService Notifications { get; set; } = default!;

    [Inject]
    private AppEvents Events { get; set; } = default!;

    private bool sidebarOpen;
    private bool notificationsOpen;
    private string theme = "light";
    private string searchText = string.Empty;
    private string userName = string.Empty;
    private bool showSignOut;
    private WatchListCounts? counts;
    private int unreadCount;
    private PeriodicTimer? pollTimer;
    private CancellationTokenSource? pollCts;

    protected override async Task OnInitializedAsync()
    {
        Navigation.LocationChanged += OnLocationChanged;
        Events.ListsChanged += OnListsChanged;
        Events.NotificationsChanged += OnNotificationsChanged;

        var state = await AuthState.GetAuthenticationStateAsync();
        userName = state.User.Identity?.Name ?? string.Empty;
        showSignOut = Auth.Provider != AuthProvider.None;

        SyncSearchFromUri();
        await RefreshCountsAsync();
        await RefreshUnreadAsync();

        pollCts = new CancellationTokenSource();
        pollTimer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        _ = PollUnreadAsync(pollCts.Token);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            theme = await Js.InvokeAsync<string>("reelsoon.getTheme");
            StateHasChanged();
        }
    }

    private bool IsActive(MediaKind kind)
    {
        string path = CurrentPath();
        return MediaKinds.TryFromRoute(path, out var current) && current == kind;
    }

    private void ToggleSidebar() => sidebarOpen = !sidebarOpen;

    private void CloseSidebar() => sidebarOpen = false;

    private void ToggleNotifications() => notificationsOpen = !notificationsOpen;

    private void CloseNotifications() => notificationsOpen = false;

    private async Task ToggleThemeAsync()
    {
        theme = theme == "dark" ? "light" : "dark";
        await Js.InvokeVoidAsync("reelsoon.setTheme", theme);
    }

    private void Search()
    {
        string route = ListRoute();
        string query = searchText.Trim();
        Navigation.NavigateTo(query.Length == 0 ? route : $"{route}?q={Uri.EscapeDataString(query)}");
        sidebarOpen = false;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        InvokeAsync(() =>
        {
            SyncSearchFromUri();
            sidebarOpen = false;
            StateHasChanged();
        });

    private void OnListsChanged() => InvokeAsync(RefreshCountsAsync);

    private void OnNotificationsChanged() => InvokeAsync(RefreshUnreadAsync);

    private void SyncSearchFromUri()
    {
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        searchText = query.TryGetValue("q", out var value) ? value.ToString() : string.Empty;
    }

    private string CurrentPath() =>
        Navigation.ToBaseRelativePath(Navigation.Uri).Split('?', '#')[0].Trim('/');

    private string ListRoute() =>
        MediaKinds.TryFromRoute(CurrentPath(), out var kind) ? kind.Route() : MediaKind.Movie.Route();

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
        try
        {
            while (pollTimer is not null && await pollTimer.WaitForNextTickAsync(cancellationToken))
            {
                await RefreshUnreadAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        Navigation.LocationChanged -= OnLocationChanged;
        Events.ListsChanged -= OnListsChanged;
        Events.NotificationsChanged -= OnNotificationsChanged;
        pollCts?.Cancel();
        pollTimer?.Dispose();
        pollCts?.Dispose();
        await Task.CompletedTask;
    }
}
