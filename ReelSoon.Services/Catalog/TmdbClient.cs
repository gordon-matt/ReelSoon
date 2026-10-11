using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReelSoon.Services.Catalog;

/// <summary>
/// TMDB search and details. Movies use <c>/search/movie</c> and <c>/movie/{id}</c>;
/// series use <c>/search/tv</c> and <c>/tv/{id}</c>. Posters are served from image.tmdb.org.
/// </summary>
internal sealed class TmdbClient(HttpClient http, IOptions<CatalogOptions> options, ILogger<TmdbClient> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.Tmdb.ApiKey);

    /// <summary>
    /// TMDB <c>/search/multi</c>, which is what Watcharr uses for a general search.
    /// Movies and series are ranked together, so a series is not hidden behind a film-only query.
    /// People and episodes are left out.
    /// </summary>
    public async Task<IReadOnlyList<CatalogHit>> SearchMultiAsync(string query, CancellationToken cancellationToken)
    {
        var page = await GetAsync<TmdbPage>($"search/multi?query={Uri.EscapeDataString(query)}", cancellationToken);
        if (page is null)
        {
            return [];
        }

        var hits = new List<CatalogHit>();
        foreach (var item in page.Results)
        {
            if (item.MediaType is not ("movie" or "tv"))
            {
                continue;
            }

            var kind = item.MediaType == "tv" ? MediaKind.TvShow : MediaKind.Movie;
            var hit = ToHit(kind, item, details: false);
            if (hit.Title.Length > 0)
            {
                hits.Add(hit);
            }
        }

        return hits;
    }

    public async Task<CatalogHit?> GetAsync(MediaKind kind, string externalId, CancellationToken cancellationToken)
    {
        string path = kind == MediaKind.TvShow ? $"tv/{Uri.EscapeDataString(externalId)}" : $"movie/{Uri.EscapeDataString(externalId)}";
        var item = await GetAsync<TmdbTitle>(path, cancellationToken);
        if (item is null || item.Id <= 0)
        {
            return null;
        }

        var hit = ToHit(kind, item, details: true);
        return hit.Title.Length == 0 ? null : hit;
    }

    public async Task<IReadOnlyList<CatalogTrailer>> GetTrailersAsync(
        MediaKind kind,
        string externalId,
        CancellationToken cancellationToken)
    {
        if (kind is not (MediaKind.Movie or MediaKind.TvShow))
        {
            return [];
        }

        string path = kind == MediaKind.TvShow
            ? $"tv/{Uri.EscapeDataString(externalId)}/videos"
            : $"movie/{Uri.EscapeDataString(externalId)}/videos";

        var payload = await GetAsync<TmdbVideos>(path, cancellationToken);
        return MapTrailers(payload);
    }

    private async Task<T?> GetAsync<T>(string relative, CancellationToken cancellationToken)
    {
        string key = Uri.EscapeDataString(options.Value.Tmdb.ApiKey);
        string separator = relative.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        string request = $"{relative}{separator}api_key={key}&language=en-US";

        using var response = await http.GetAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("TMDB {Path} returned {StatusCode}", relative.Split('?')[0], (int)response.StatusCode);
            throw new CatalogRequestException(response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "TMDB rejected the API key in Catalog:Tmdb:ApiKey."
                : "The movie and TV catalog is unavailable right now.");
        }

        return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
    }

    private static CatalogHit ToHit(MediaKind kind, TmdbTitle item, bool details)
    {
        string title = CatalogText.Title(string.IsNullOrWhiteSpace(item.Title) ? item.Name : item.Title);
        string? dateText = kind == MediaKind.TvShow ? item.FirstAirDate : item.ReleaseDate;
        string fallback = kind == MediaKind.TvShow
            ? $"https://www.themoviedb.org/tv/{item.Id}"
            : $"https://www.themoviedb.org/movie/{item.Id}";

        return new CatalogHit(
            kind,
            CatalogSource.Tmdb,
            item.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            title,
            CatalogText.Clip(item.Overview, Constants.MaxLengths.Notes),
            CatalogText.Date(dateText),
            TmdbPoster(item.PosterPath),
            details ? CatalogText.HttpUrl(item.Homepage) ?? fallback : fallback);
    }

    private static string? TmdbPoster(string? path) =>
        string.IsNullOrWhiteSpace(path) ? null : $"https://image.tmdb.org/t/p/w342{path}";

    private static IReadOnlyList<CatalogTrailer> MapTrailers(TmdbVideos? payload)
    {
        if (payload is null)
        {
            return [];
        }

        var candidates = payload.Results
            .Where(video => video.Type is "Trailer" or "Teaser")
            .Select(video => (Video: video, Url: WatchUrl(video)))
            .Where(pair => pair.Url is not null)
            .ToList();

        // TMDB files promo spots under type Trailer. Prefer clips whose name says trailer or teaser.
        var named = candidates.Where(pair => IsTrailerName(pair.Video.Name)).ToList();
        var chosen = named.Count > 0
            ? named
            : candidates.Where(pair => pair.Video.Official && pair.Video.Type == "Trailer").ToList();
        if (chosen.Count == 0)
        {
            chosen = candidates.Where(pair => pair.Video.Type == "Trailer").ToList();
        }

        return chosen
            .OrderBy(pair => pair.Video.Type == "Trailer" ? 0 : 1)
            .ThenBy(pair => pair.Video.Official ? 0 : 1)
            .ThenBy(pair => pair.Video.Language == "en" ? 0 : 1)
            .Take(5)
            .Select(pair => new CatalogTrailer(
                string.IsNullOrWhiteSpace(pair.Video.Name) ? pair.Video.Type ?? "Trailer" : pair.Video.Name.Trim(),
                pair.Url!))
            .ToList();
    }

    private static bool IsTrailerName(string? name) =>
        name?.Contains("trailer", StringComparison.OrdinalIgnoreCase) == true
        || name?.Contains("teaser", StringComparison.OrdinalIgnoreCase) == true;

    private static string? WatchUrl(TmdbVideo video)
    {
        if (string.IsNullOrWhiteSpace(video.Key))
        {
            return null;
        }

        string key = Uri.EscapeDataString(video.Key);
        return video.Site?.ToLowerInvariant() switch
        {
            "youtube" => $"https://www.youtube.com/watch?v={key}",
            "vimeo" => $"https://vimeo.com/{key}",
            _ => null,
        };
    }

    private sealed class TmdbPage
    {
        public List<TmdbTitle> Results { get; set; } = [];
    }

    private sealed class TmdbTitle
    {
        public int Id { get; set; }

        public string? Title { get; set; }

        public string? Name { get; set; }

        public string? Overview { get; set; }

        public string? Homepage { get; set; }

        [JsonPropertyName("media_type")]
        public string? MediaType { get; set; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }
    }

    private sealed class TmdbVideos
    {
        public List<TmdbVideo> Results { get; set; } = [];
    }

    private sealed class TmdbVideo
    {
        public string? Name { get; set; }

        public string? Key { get; set; }

        public string? Site { get; set; }

        public string? Type { get; set; }

        public bool Official { get; set; }

        [JsonPropertyName("iso_639_1")]
        public string? Language { get; set; }
    }
}
