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

    public async Task<IReadOnlyList<CatalogHit>> SearchAsync(MediaKind kind, string query, CancellationToken cancellationToken)
    {
        string path = kind == MediaKind.TvShow ? "search/tv" : "search/movie";
        var page = await GetAsync<TmdbPage>($"{path}?query={Uri.EscapeDataString(query)}", cancellationToken);
        return page?.Results.Select(item => ToHit(kind, item, details: false)).Where(hit => hit.Title.Length > 0).ToList()
            ?? [];
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

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("first_air_date")]
        public string? FirstAirDate { get; set; }
    }
}
