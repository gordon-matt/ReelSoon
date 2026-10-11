using Microsoft.AspNetCore.Components;

namespace ReelSoon.Components.Shared;

public partial class ItemDrawer
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

    private string StatusClass =>
        Item is null || Item.ReleaseDate is null
            ? "rs-badge-muted"
            : Item.IsReleased ? "rs-badge-success" : "rs-badge-upcoming";
}
