namespace ReelSoon;

public enum MediaKind
{
    Movie = 0,
    TvShow = 1,
    Game = 2,
}

public static class MediaKinds
{
    public static readonly IReadOnlyList<MediaKind> All = [MediaKind.Movie, MediaKind.TvShow, MediaKind.Game];

    public static string Label(this MediaKind kind) => kind switch
    {
        MediaKind.Movie => "Movies",
        MediaKind.TvShow => "TV Shows",
        MediaKind.Game => "Games",
        _ => kind.ToString(),
    };

    public static string Singular(this MediaKind kind) => kind switch
    {
        MediaKind.Movie => "Movie",
        MediaKind.TvShow => "TV show",
        MediaKind.Game => "Game",
        _ => kind.ToString(),
    };

    public static string Route(this MediaKind kind) => kind switch
    {
        MediaKind.Movie => "/movies",
        MediaKind.TvShow => "/tv",
        MediaKind.Game => "/games",
        _ => "/movies",
    };

    public static string Icon(this MediaKind kind) => kind switch
    {
        MediaKind.Movie => "film",
        MediaKind.TvShow => "tv",
        MediaKind.Game => "game",
        _ => "film",
    };

    public static bool TryFromRoute(string? segment, out MediaKind kind)
    {
        switch (segment?.Trim('/').ToLowerInvariant())
        {
            case "movies":
            case "":
                kind = MediaKind.Movie;
                return true;
            case "tv":
                kind = MediaKind.TvShow;
                return true;
            case "games":
                kind = MediaKind.Game;
                return true;
            default:
                kind = default;
                return false;
        }
    }
}
