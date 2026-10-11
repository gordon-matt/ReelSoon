using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using ReelSoon.Components.Shared;
using ReelSoon.Services.Catalog;

namespace ReelSoon.Components.Pages;

public partial class Watch : IDisposable
{
    private enum ListFilter
    {
        All,
        Upcoming,
        Released,
    }

    [Inject]
    private IWatchItemService WatchItems { get; set; } = default!;

    [Inject]
    private ICatalogService Catalog { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ToastService Toasts { get; set; } = default!;

    [Inject]
    private AppEvents Events { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "q")]
    public string? Query { get; set; }

    [SupplyParameterFromQuery(Name = "id")]
    public int? SelectedId { get; set; }

    private MediaKind kind = MediaKind.Movie;
    private string? loadedSearch;
    private bool loaded;
    private bool loading = true;
    private string? loadError;
    private List<WatchItemDto> items = [];
    private ListFilter filter = ListFilter.All;

    private WatchItemDto? selected;
    private bool creating;
    private bool editing;
    private bool saving;
    private string? formError;
    private WatchItemForm form = new();
    private ConfirmDialog confirm = default!;

    private List<CatalogHit> hits = [];
    private CatalogHit? preview;
    private int previewGeneration;
    private bool searching;
    private bool searched;
    private bool manual;
    private string? searchError;
    private int searchGeneration;
    private int routeGeneration;
    private int trailerGeneration;
    private int? trailersForId;
    private List<CatalogTrailer> trailers = [];

    private bool drawerOpen => creating || selected is not null;

    private IReadOnlyList<CatalogTrailer> DrawerTrailers =>
        preview is not null ? preview.Trailers : trailers;

    private DateOnly Today => DateOnly.FromDateTime(Time.GetLocalNow().DateTime);

    private IReadOnlyList<WatchItemDto> visible => filter switch
    {
        ListFilter.Upcoming => items.Where(item => item.ReleaseDate is not null && !item.IsReleased).ToList(),
        ListFilter.Released => items.Where(item => item.IsReleased).ToList(),
        _ => items,
    };

    private string Summary
    {
        get
        {
            string noun = items.Count == 1 ? kind.Singular().ToLowerInvariant() : kind.Label().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(Query))
            {
                return $"{visible.Count} of {items.Count} {noun} match “{Query.Trim()}”.";
            }

            int upcoming = items.Count(item => item.ReleaseDate is not null && !item.IsReleased);
            int released = items.Count(item => item.IsReleased);
            return $"{items.Count} {noun} · {upcoming} coming up · {released} released";
        }
    }

    private string EmptyTitle =>
        string.IsNullOrWhiteSpace(Query)
            ? $"No {kind.Label().ToLowerInvariant()} yet"
            : "Nothing matches";

    private string EmptyBody =>
        string.IsNullOrWhiteSpace(Query)
            ? $"Add a {kind.Singular().ToLowerInvariant()} and ReelSoon will tell you when it is out."
            : "Try a shorter title, or clear the search.";

    protected override void OnInitialized()
    {
        Events.NotificationsChanged += OnNotified;
        Navigation.LocationChanged += OnLocationChanged;
    }

    /// <summary>
    /// Movies, TV shows, and games are the same component. Blazor only calls
    /// <see cref="OnParametersSetAsync"/> when a parameter changes, so a plain
    /// /movies to /tv click updates the address bar and leaves the old list on screen.
    /// </summary>
    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        InvokeAsync(async () =>
        {
            await SyncRouteAsync();
            StateHasChanged();
        });

    private void OnNotified() =>
        InvokeAsync(async () =>
        {
            if (!loaded)
            {
                return;
            }

            await LoadAsync(loadedSearch);
            ApplySelection(ReadRouteQuery().Id);
            StateHasChanged();
        });

    public void Dispose()
    {
        Events.NotificationsChanged -= OnNotified;
        Navigation.LocationChanged -= OnLocationChanged;
    }

    protected override Task OnParametersSetAsync() => SyncRouteAsync();

    private async Task SyncRouteAsync()
    {
        int generation = Interlocked.Increment(ref routeGeneration);
        string path = Navigation.ToBaseRelativePath(Navigation.Uri).Split('?', '#')[0].Trim('/');
        if (!MediaKinds.TryFromRoute(path, out var next))
        {
            next = MediaKind.Movie;
        }

        var (search, selectedId) = ReadRouteQuery();
        bool listChanged = !loaded || next != kind || !string.Equals(search, loadedSearch, StringComparison.Ordinal);

        if (next != kind)
        {
            filter = ListFilter.All;
            creating = false;
            editing = false;
            selected = null;
            trailers = [];
            trailersForId = null;
            items = [];
            ResetCatalogSearch();
        }

        kind = next;

        if (listChanged)
        {
            // A second caller (location event plus parameter set) must still load.
            // Committing the result is what the generation check below protects.
            loaded = false;
            await LoadAsync(search, generation);
        }

        if (generation != routeGeneration)
        {
            return;
        }

        ApplySelection(selectedId);
        await LoadTrailersAsync(selected);
    }

    private (string? Search, int? Id) ReadRouteQuery()
    {
        var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);
        string? search = query.TryGetValue("q", out var term) && !string.IsNullOrWhiteSpace(term)
            ? term.ToString().Trim()
            : null;
        int? id = query.TryGetValue("id", out var raw) && int.TryParse(raw, out int parsed) ? parsed : null;
        return (search, id);
    }

    private async Task LoadAsync(string? search, int? generation = null)
    {
        loading = items.Count == 0;
        loadError = null;

        var listed = await WatchItems.ListAsync(kind, search);
        if (generation is int token && token != routeGeneration)
        {
            return;
        }

        loaded = true;
        loadedSearch = search;

        if (!listed.IsSuccess)
        {
            loadError = listed.GetDisplayMessage();
            items = [];
            loading = false;
            return;
        }

        items = [.. listed.Value];
        loading = false;
    }

    private void ApplySelection(int? id)
    {
        if (creating)
        {
            return;
        }

        if (id is not int)
        {
            selected = null;
            editing = false;
            return;
        }

        var match = items.FirstOrDefault(item => item.Id == id);
        if (match is null)
        {
            selected = null;
            return;
        }

        if (selected?.Id != match.Id)
        {
            editing = false;
            formError = null;
        }

        selected = match;
    }

    private void Open(WatchItemDto item)
    {
        creating = false;
        editing = false;
        formError = null;
        Navigation.NavigateTo(ItemUrl(item.Id));
    }

    private void StartCreate()
    {
        creating = true;
        editing = true;
        selected = null;
        form = new WatchItemForm();
        formError = null;
        ResetCatalogSearch();

        if (SelectedId is not null)
        {
            Navigation.NavigateTo(ListUrl());
        }
    }

    private void CloseDrawer()
    {
        creating = false;
        editing = false;
        selected = null;
        formError = null;

        if (SelectedId is not null)
        {
            Navigation.NavigateTo(ListUrl());
        }
    }

    private void BeginEdit()
    {
        if (selected is null)
        {
            return;
        }

        form = new WatchItemForm
        {
            Title = selected.Title,
            ReleaseDate = selected.ReleaseDate,
            Notes = selected.Notes,
            LinkUrl = selected.LinkUrl,
            ImageUrl = selected.ImageUrl,
        };
        formError = null;
        editing = true;
    }

    private void CancelEdit()
    {
        if (creating)
        {
            CloseDrawer();
            return;
        }

        editing = false;
        formError = null;
    }

    private void ShowManual() => manual = true;

    private void ResetCatalogSearch()
    {
        searchGeneration++;
        hits = [];
        searching = false;
        searched = false;
        manual = false;
        searchError = null;
        preview = null;
        previewGeneration++;
    }

    private async Task SearchCatalogAsync(string query)
    {
        int generation = ++searchGeneration;
        string term = query.Trim();

        if (term.Length < 2)
        {
            hits = [];
            searching = false;
            searched = false;
            searchError = null;
            return;
        }

        searching = true;
        searched = false;
        searchError = null;

        var result = await Catalog.SearchAsync(kind, term);
        if (generation != searchGeneration)
        {
            return;
        }

        searching = false;
        searched = true;

        if (!result.IsSuccess)
        {
            hits = [];
            searchError = result.GetDisplayMessage();
            return;
        }

        hits = [.. result.Value];
    }

    private async Task PreviewAsync(CatalogHit hit)
    {
        int generation = ++previewGeneration;
        preview = hit;
        searchError = null;

        var fresh = await Catalog.GetAsync(hit.Kind, hit.Source, hit.ExternalId);
        if (generation != previewGeneration)
        {
            return;
        }

        var chosen = fresh.IsSuccess ? fresh.Value : hit;
        if (hit.Kind is MediaKind.Movie or MediaKind.TvShow)
        {
            var videos = await Catalog.GetTrailersAsync(hit.Kind, hit.ExternalId);
            if (generation != previewGeneration)
            {
                return;
            }

            if (videos.IsSuccess)
            {
                chosen = chosen with { Trailers = videos.Value };
            }
        }

        preview = chosen;
    }

    private async Task LoadTrailersAsync(WatchItemDto? item)
    {
        if (item is null
            || item.CatalogSource != CatalogSource.Tmdb
            || item.Kind is not (MediaKind.Movie or MediaKind.TvShow)
            || string.IsNullOrWhiteSpace(item.ExternalId))
        {
            trailers = [];
            trailersForId = item?.Id;
            return;
        }

        if (trailersForId == item.Id)
        {
            return;
        }

        int generation = Interlocked.Increment(ref trailerGeneration);
        trailersForId = item.Id;
        trailers = [];

        var videos = await Catalog.GetTrailersAsync(item.Kind, item.ExternalId);
        if (generation != trailerGeneration)
        {
            return;
        }

        trailers = videos.IsSuccess ? [.. videos.Value] : [];
    }

    private void BackToResults()
    {
        previewGeneration++;
        preview = null;
        searchError = null;
    }

    private Task AddPreviewAsync() =>
        preview is null ? Task.CompletedTask : PickAsync(preview);

    private async Task PickAsync(CatalogHit hit)
    {
        saving = true;
        formError = null;

        var added = await WatchItems.AddFromCatalogAsync(kind, hit);
        saving = false;

        if (!added.IsSuccess)
        {
            searchError = added.GetDisplayMessage();
            return;
        }

        var saved = added.Value.Item;
        if (added.Value.AlreadyListed)
        {
            Toasts.Show($"{saved.Title} is already on your {saved.Kind.Label().ToLowerInvariant()}.");
        }
        else if (saved.Kind != kind)
        {
            Toasts.Success($"Added {saved.Title} to {saved.Kind.Label()}.");
        }
        else
        {
            Toasts.Success($"Added {saved.Title}.");
        }

        Events.RaiseListsChanged();
        Events.RaiseNotificationsChanged();
        creating = false;
        editing = false;
        ResetCatalogSearch();

        if (saved.Kind == kind)
        {
            await LoadAsync(loadedSearch);
            Navigation.NavigateTo(ItemUrl(saved.Id));
            return;
        }

        Navigation.NavigateTo($"{saved.Kind.Route()}?id={saved.Id}");
    }

    private async Task SaveAsync()
    {
        saving = true;
        formError = null;

        if (creating)
        {
            var created = await WatchItems.CreateAsync(kind, form);
            saving = false;

            if (!created.IsSuccess)
            {
                formError = created.GetDisplayMessage();
                return;
            }

            Toasts.Success($"Added {created.Value.Title}.");
            Events.RaiseListsChanged();
            Events.RaiseNotificationsChanged();
            creating = false;
            editing = false;
            await LoadAsync(loadedSearch);
            Navigation.NavigateTo(ItemUrl(created.Value.Id));
            return;
        }

        if (selected is null)
        {
            saving = false;
            return;
        }

        var updated = await WatchItems.UpdateAsync(selected.Id, form);
        saving = false;

        if (!updated.IsSuccess)
        {
            formError = updated.GetDisplayMessage();
            return;
        }

        Replace(updated.Value);
        selected = updated.Value;
        editing = false;
        Toasts.Success("Saved.");
        Events.RaiseListsChanged();
        Events.RaiseNotificationsChanged();
    }

    private async Task DeleteAsync()
    {
        if (selected is null)
        {
            return;
        }

        bool confirmed = await confirm.ShowAsync(
            $"Remove {selected.Title}?",
            "It leaves your list, and any release notification for it goes with it.");

        if (!confirmed)
        {
            return;
        }

        var deleted = await WatchItems.DeleteAsync(selected.Id);
        if (!deleted.IsSuccess)
        {
            Toasts.Error(deleted.GetDisplayMessage());
            return;
        }

        Toasts.Success($"Removed {selected.Title}.");
        Events.RaiseListsChanged();
        Events.RaiseNotificationsChanged();
        creating = false;
        editing = false;
        selected = null;
        await LoadAsync(loadedSearch);
        Navigation.NavigateTo(ListUrl());
    }

    private void Replace(WatchItemDto updated)
    {
        int index = items.FindIndex(item => item.Id == updated.Id);
        if (index >= 0)
        {
            items[index] = updated;
        }
    }

    private string ListUrl() =>
        string.IsNullOrWhiteSpace(Query)
            ? kind.Route()
            : $"{kind.Route()}?q={Uri.EscapeDataString(Query.Trim())}";

    private string ItemUrl(int id)
    {
        string query = string.IsNullOrWhiteSpace(Query)
            ? $"id={id}"
            : $"id={id}&q={Uri.EscapeDataString(Query.Trim())}";

        return $"{kind.Route()}?{query}";
    }

    private static string StatusClass(WatchItemDto item) =>
        item.ReleaseDate is null ? "rs-badge-muted" : item.IsReleased ? "rs-badge-success" : "rs-badge-upcoming";
}
