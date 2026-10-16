using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReelSoon.Services.Catalog;

/// <summary>
/// IGDB game search. Watcharr does the same thing: a Twitch client-credentials token,
/// then an APICalypse query against <c>/games</c> for name, cover, summary, and first release date.
/// </summary>
internal sealed class IgdbClient(
    HttpClient http,
    IgdbSession session,
    IOptions<CatalogOptions> options,
    ILogger<IgdbClient> logger)
{
    private const string Fields = "name,cover.image_id,summary,first_release_date,url";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(options.Value.Igdb.ClientId)
        && !string.IsNullOrWhiteSpace(options.Value.Igdb.ClientSecret);

    public async Task<IReadOnlyList<CatalogHit>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        string body = $"fields {Fields}; search \"{Escape(query)}\"; limit 20;";
        var games = await PostAsync(body, cancellationToken);
        return games.Select(ToHit).Where(hit => hit.Title.Length > 0).ToList();
    }

    public async Task<CatalogHit?> GetAsync(string externalId, CancellationToken cancellationToken)
    {
        if (!long.TryParse(externalId, out long id) || id <= 0)
        {
            return null;
        }

        string body = $"fields {Fields}; where id = {id};";
        var games = await PostAsync(body, cancellationToken);
        var hit = games.Select(ToHit).FirstOrDefault(candidate => candidate.Title.Length > 0);
        return hit;
    }

    private async Task<List<IgdbGame>> PostAsync(string body, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(body, cancellationToken, refreshToken: false);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            using var retry = await SendAsync(body, cancellationToken, refreshToken: true);
            return await ReadAsync(retry, cancellationToken);
        }

        return await ReadAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(string body, CancellationToken cancellationToken, bool refreshToken)
    {
        string token = await session.GetTokenAsync(refreshToken, cancellationToken);
        var request = new HttpRequestMessage(HttpMethod.Post, "games")
        {
            Content = new StringContent(body, Encoding.UTF8, "text/plain"),
        };
        request.Headers.TryAddWithoutValidation("Client-ID", options.Value.Igdb.ClientId);
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
        return await http.SendAsync(request, cancellationToken);
    }

    private async Task<List<IgdbGame>> ReadAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("IGDB returned {StatusCode}", (int)response.StatusCode);
            throw new CatalogRequestException(response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "IGDB rejected the Twitch client id or secret in Catalog:Igdb."
                : "The game catalog is unavailable right now.");
        }

        return await response.Content.ReadFromJsonAsync<List<IgdbGame>>(Json, cancellationToken) ?? [];
    }

    private static CatalogHit ToHit(IgdbGame game)
    {
        string? cover = string.IsNullOrWhiteSpace(game.Cover?.ImageId)
            ? null
            : $"https://images.igdb.com/igdb/image/upload/t_cover_big/{game.Cover.ImageId}.jpg";

        return new CatalogHit(
            MediaKind.Game,
            CatalogSource.Igdb,
            game.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            CatalogText.Title(game.Name),
            CatalogText.Clip(game.Summary, Constants.MaxLengths.Notes),
            CatalogText.UnixDate(game.FirstReleaseDate),
            cover,
            CatalogText.HttpUrl(game.Url) ?? $"https://www.igdb.com/games/{game.Id}");
    }

    private static string Escape(string query)
    {
        string cleaned = query.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        if (cleaned.Length > 120)
        {
            cleaned = cleaned[..120];
        }

        return cleaned.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    private sealed class IgdbGame
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Summary { get; set; }

        public string? Url { get; set; }

        public IgdbCover? Cover { get; set; }

        [JsonPropertyName("first_release_date")]
        public long? FirstReleaseDate { get; set; }
    }

    private sealed class IgdbCover
    {
        [JsonPropertyName("image_id")]
        public string? ImageId { get; set; }
    }
}

/// <summary>Holds the Twitch access token IGDB requires, refreshing it shortly before it expires.</summary>
internal sealed class IgdbSession(IHttpClientFactory httpFactory, IOptions<CatalogOptions> options)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private string? token;
    private DateTimeOffset expires;

    public async Task<string> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!forceRefresh && token is not null && DateTimeOffset.UtcNow < expires)
            {
                return token;
            }

            var igdb = options.Value.Igdb;
            string url =
                "https://id.twitch.tv/oauth2/token" +
                $"?client_id={Uri.EscapeDataString(igdb.ClientId)}" +
                $"&client_secret={Uri.EscapeDataString(igdb.ClientSecret)}" +
                "&grant_type=client_credentials";

            var http = httpFactory.CreateClient(nameof(IgdbSession));
            using var response = await http.PostAsync(url, content: null, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new CatalogRequestException("IGDB rejected the Twitch client id or secret in Catalog:Igdb.");
            }

            var payload = await response.Content.ReadFromJsonAsync<TwitchToken>(cancellationToken)
                ?? throw new CatalogRequestException("IGDB did not return an access token.");

            if (string.IsNullOrWhiteSpace(payload.AccessToken))
            {
                throw new CatalogRequestException("IGDB did not return an access token.");
            }

            token = payload.AccessToken;
            expires = DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, payload.ExpiresIn - 60));
            return token;
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed class TwitchToken
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}

internal sealed class CatalogRequestException(string message) : Exception(message);