namespace ReelSoon.Services.Catalog;

public sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    public TmdbOptions Tmdb { get; set; } = new();

    public IgdbOptions Igdb { get; set; } = new();
}

/// <summary>TMDB v3 key used for movies and TV series. Create one at themoviedb.org.</summary>
public sealed class TmdbOptions
{
    public string ApiKey { get; set; } = string.Empty;
}

/// <summary>
/// Twitch application credentials used to call IGDB for games.
/// Create an application at dev.twitch.tv and copy the client id and secret.
/// </summary>
public sealed class IgdbOptions
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;
}
