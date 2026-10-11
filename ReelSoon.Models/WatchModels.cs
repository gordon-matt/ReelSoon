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
    DateOnly? ReleaseDate,
    string? LinkUrl,
    string? ImageUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsReleased);

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
