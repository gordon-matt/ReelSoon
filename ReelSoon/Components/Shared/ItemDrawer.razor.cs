using Microsoft.AspNetCore.Components;

namespace ReelSoon.Components.Shared;

public partial class ItemDrawer : IDisposable
{
    [Parameter]
    public MediaKind Kind { get; set; }

    [Parameter]
    public WatchItemDto? Item { get; set; }

    [Parameter]
    public bool Creating { get; set; }

    [Parameter]
    public bool Editing { get; set; }

    [Parameter, EditorRequired]
    public WatchItemForm Form { get; set; } = new();

    [Parameter]
    public DateOnly Today { get; set; }

    [Parameter]
    public bool Saving { get; set; }

    [Parameter]
    public string? Error { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Parameter]
    public EventCallback OnEdit { get; set; }

    [Parameter]
    public EventCallback OnCancelEdit { get; set; }

    [Parameter]
    public EventCallback OnSave { get; set; }

    [Parameter]
    public EventCallback OnDelete { get; set; }

    [Parameter]
    public IReadOnlyList<CatalogHit> Hits { get; set; } = [];

    [Parameter]
    public bool Searching { get; set; }

    [Parameter]
    public bool Searched { get; set; }

    [Parameter]
    public string? SearchError { get; set; }

    [Parameter]
    public bool Manual { get; set; }

    [Parameter]
    public bool CatalogLinked { get; set; }

    [Parameter]
    public EventCallback<string> OnSearch { get; set; }

    [Parameter]
    public EventCallback<CatalogHit> OnPreview { get; set; }

    [Parameter]
    public CatalogHit? Preview { get; set; }

    [Parameter]
    public EventCallback OnBack { get; set; }

    [Parameter]
    public EventCallback OnAddPreview { get; set; }

    [Parameter]
    public EventCallback OnShowManual { get; set; }

    private string query = string.Empty;
    private CancellationTokenSource? debounce;

    private string SearchPlaceholder => Kind switch
    {
        MediaKind.TvShow => "Search for a TV show",
        MediaKind.Game => "Search for a game",
        _ => "Search for a movie",
    };

    private string SearchHint => Kind == MediaKind.Game
        ? "Results come from IGDB, including the cover and release date."
        : "Movies and TV shows are searched together, so a series is not hidden by a film with a similar name.";

    private async Task OnQueryInput(ChangeEventArgs args)
    {
        query = args.Value?.ToString() ?? string.Empty;
        debounce?.Cancel();
        debounce?.Dispose();
        debounce = new CancellationTokenSource();
        var token = debounce.Token;

        try
        {
            await Task.Delay(350, token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await OnSearch.InvokeAsync(query);
    }

    public void Dispose()
    {
        debounce?.Cancel();
        debounce?.Dispose();
    }

    private string PreviewStatusClass =>
        Preview is null || Preview.ReleaseDate is null
            ? "rs-badge-muted"
            : ReleaseText.IsReleased(Preview.ReleaseDate, Today) ? "rs-badge-success" : "rs-badge-upcoming";

    private string StatusClass =>
        Item is null || Item.ReleaseDate is null
            ? "rs-badge-muted"
            : Item.IsReleased ? "rs-badge-success" : "rs-badge-upcoming";
}
