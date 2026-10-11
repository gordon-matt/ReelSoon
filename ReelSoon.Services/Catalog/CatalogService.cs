namespace ReelSoon.Services.Catalog;

internal sealed class CatalogService(
    TmdbClient tmdb,
    IgdbClient igdb,
    IMemoryCache cache,
    ILogger<CatalogService> logger) : ICatalogService
{
    private static readonly TimeSpan SearchCacheLifetime = TimeSpan.FromMinutes(10);

    public async Task<Result<IReadOnlyList<CatalogHit>>> SearchAsync(
        MediaKind kind,
        string query,
        CancellationToken cancellationToken = default)
    {
        string term = query.Trim();
        if (term.Length < 2)
        {
            return Result<IReadOnlyList<CatalogHit>>.Success([]);
        }

        string cacheKey = $"catalog:{kind}:{term.ToLowerInvariant()}";
        if (cache.TryGetValue(cacheKey, out IReadOnlyList<CatalogHit>? cached) && cached is not null)
        {
            return Result<IReadOnlyList<CatalogHit>>.Success(cached);
        }

        try
        {
            IReadOnlyList<CatalogHit> hits = kind switch
            {
                MediaKind.Game => await SearchGamesAsync(term, cancellationToken),
                _ => await SearchTmdbAsync(kind, term, cancellationToken),
            };

            cache.Set(cacheKey, hits, SearchCacheLifetime);
            return Result<IReadOnlyList<CatalogHit>>.Success(hits);
        }
        catch (CatalogRequestException ex)
        {
            logger.LogWarning(ex, "Catalog search failed for {Kind}", kind);
            return Result<IReadOnlyList<CatalogHit>>.Error(ex.Message);
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Catalog search failed for {Kind}", kind);
            return Result<IReadOnlyList<CatalogHit>>.Error(Unavailable(kind));
        }
    }

    public async Task<Result<CatalogHit>> GetAsync(
        MediaKind kind,
        CatalogSource source,
        string externalId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CatalogHit? hit = source switch
            {
                CatalogSource.Igdb => await igdb.GetAsync(externalId, cancellationToken),
                _ => await tmdb.GetAsync(kind, externalId, cancellationToken),
            };

            return hit is null
                ? Result<CatalogHit>.NotFound("That title is no longer in the catalog.")
                : Result<CatalogHit>.Success(hit);
        }
        catch (CatalogRequestException ex)
        {
            return Result<CatalogHit>.Error(ex.Message);
        }
        catch (Exception ex) when (IsTransportFailure(ex, cancellationToken))
        {
            logger.LogWarning(ex, "Catalog lookup failed for {Source} {Id}", source, externalId);
            return Result<CatalogHit>.Error(Unavailable(kind));
        }
    }

    private async Task<IReadOnlyList<CatalogHit>> SearchTmdbAsync(MediaKind kind, string query, CancellationToken cancellationToken)
    {
        if (!tmdb.IsConfigured)
        {
            throw new CatalogRequestException(
                "Movie and TV search needs a TMDB API key in Catalog:Tmdb:ApiKey. You can still add a title yourself.");
        }

        return await tmdb.SearchAsync(kind, query, cancellationToken);
    }

    private async Task<IReadOnlyList<CatalogHit>> SearchGamesAsync(string query, CancellationToken cancellationToken)
    {
        if (!igdb.IsConfigured)
        {
            throw new CatalogRequestException(
                "Game search needs a Twitch client id and secret in Catalog:Igdb. You can still add a game yourself.");
        }

        return await igdb.SearchAsync(query, cancellationToken);
    }

    private static bool IsTransportFailure(Exception ex, CancellationToken cancellationToken) =>
        ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested);

    private static string Unavailable(MediaKind kind) =>
        kind == MediaKind.Game
            ? "The game catalog is unavailable right now."
            : "The movie and TV catalog is unavailable right now.";
}
