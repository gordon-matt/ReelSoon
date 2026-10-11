namespace ReelSoon.Models;

public sealed class WatchItemForm
{
    [Required(ErrorMessage = "Give it a title.")]
    [StringLength(Constants.MaxLengths.Title)]
    public string Title { get; set; } = string.Empty;

    public DateOnly? ReleaseDate { get; set; }

    [StringLength(Constants.MaxLengths.Notes)]
    public string? Notes { get; set; }

    [StringLength(Constants.MaxLengths.Url)]
    public string? LinkUrl { get; set; }

    [StringLength(Constants.MaxLengths.Url)]
    public string? ImageUrl { get; set; }
}

public sealed record WatchItemDto(
    int Id,
    MediaKind Kind,
    string Title,
    string? Notes,
    string? Overview,
    DateOnly? ReleaseDate,
    string? LinkUrl,
    string? ImageUrl,
    CatalogSource? CatalogSource,
    string? ExternalId,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsReleased);

/// <summary>A movie, series, or game returned by an online catalog search.</summary>
public sealed record CatalogHit(
    CatalogSource Source,
    string ExternalId,
    string Title,
    string? Overview,
    DateOnly? ReleaseDate,
    string? ImageUrl,
    string? LinkUrl);

/// <summary>Result of adding a catalog hit. <see cref="AlreadyListed"/> means it was already on the list.</summary>
public sealed record CatalogAdd(WatchItemDto Item, bool AlreadyListed);

public sealed record WatchListCounts(int Movies, int TvShows, int Games)
{
    public int For(MediaKind kind) => kind switch
    {
        MediaKind.Movie => Movies,
        MediaKind.TvShow => TvShows,
        MediaKind.Game => Games,
        _ => 0,
    };
}

public sealed record WatchNotificationDto(
    int Id,
    int WatchItemId,
    MediaKind Kind,
    string Title,
    string Message,
    DateTime CreatedAt,
    DateTime? ReadAt)
{
    public bool IsUnread => ReadAt is null;
}
