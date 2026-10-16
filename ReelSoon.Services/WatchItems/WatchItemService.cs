using ReelSoon.Services.Catalog;
using ReelSoon.Services.Notifications;

namespace ReelSoon.Services.WatchItems;

public sealed class WatchItemService(
    ILogger<WatchItemService> logger,
    IUserContextService userContext,
    IRepository<WatchItem> items,
    IRepository<WatchNotification> notifications,
    IReleaseNotificationService releases,
    ICatalogService catalog,
    TimeProvider time) : IWatchItemService
{
    public async Task<Result<IReadOnlyList<WatchItemDto>>> ListAsync(
        MediaKind kind,
        string? search,
        CancellationToken cancellationToken = default)
    {
        if (ResolveOwner() is not string ownerUserId)
        {
            return Result<IReadOnlyList<WatchItemDto>>.Unauthorized();
        }

        string? term = string.IsNullOrWhiteSpace(search) ? null : search.Trim().ToLowerInvariant();

        var page = await items.FindAsync(new SearchOptions<WatchItem>
        {
            Query = item => item.OwnerUserId == ownerUserId
                && item.Kind == kind
                && (term == null || item.Title.ToLower().Contains(term)),
            CancellationToken = cancellationToken,
        });

        var today = Today();
        var ordered = page
            .Select(item => ToDto(item, today))
            .OrderBy(item => Rank(item, today))
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Result<IReadOnlyList<WatchItemDto>>.Success(ordered);
    }

    public async Task<Result<WatchListCounts>> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        if (ResolveOwner() is not string ownerUserId)
        {
            return Result<WatchListCounts>.Unauthorized();
        }

        var options = new ContextOptions { CancellationToken = cancellationToken };

        int movies = await items.CountAsync(item => item.OwnerUserId == ownerUserId && item.Kind == MediaKind.Movie, options);
        int shows = await items.CountAsync(item => item.OwnerUserId == ownerUserId && item.Kind == MediaKind.TvShow, options);
        int games = await items.CountAsync(item => item.OwnerUserId == ownerUserId && item.Kind == MediaKind.Game, options);

        return Result<WatchListCounts>.Success(new WatchListCounts(movies, shows, games));
    }

    public async Task<Result<WatchItemDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var found = await FindOwnedAsync(id, cancellationToken);
        return found.IsSuccessStatus()
            ? Result<WatchItemDto>.Success(ToDto(found.Value, Today()))
            : found.To<WatchItemDto>();
    }

    public async Task<Result<WatchItemDto>> CreateAsync(
        MediaKind kind,
        WatchItemForm form,
        CancellationToken cancellationToken = default)
    {
        if (ResolveOwner() is not string ownerUserId)
        {
            return Result<WatchItemDto>.Unauthorized();
        }

        var validated = Validate(form);
        if (!validated.IsSuccessStatus())
        {
            return validated.To<WatchItemDto>();
        }

        var now = time.GetUtcNow().UtcDateTime;
        var created = await items.InsertAsync(new WatchItem
        {
            Kind = kind,
            Title = form.Title.Trim(),
            Notes = BlankToNull(form.Notes),
            ReleaseDate = form.ReleaseDate,
            LinkUrl = NormalizeUrl(form.LinkUrl),
            ImageUrl = NormalizeUrl(form.ImageUrl),
            OwnerUserId = ownerUserId,
            CreatedAt = now,
            UpdatedAt = now,
        });

        logger.LogInformation("Added {Kind} '{Title}' for {UserId}", kind, created.Title, ownerUserId);

        await releases.NotifyDueAsync(cancellationToken);

        var saved = await FindOwnedAsync(created.Id, cancellationToken);
        return saved.IsSuccessStatus()
            ? Result<WatchItemDto>.Success(ToDto(saved.Value, Today()))
            : Result<WatchItemDto>.Success(ToDto(created, Today()));
    }

    public async Task<Result<CatalogAdd>> AddFromCatalogAsync(
        MediaKind kind,
        CatalogHit hit,
        CancellationToken cancellationToken = default)
    {
        if (ResolveOwner() is not string ownerUserId)
        {
            return Result<CatalogAdd>.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(hit.ExternalId) || string.IsNullOrWhiteSpace(hit.Title))
        {
            return Result<CatalogAdd>.Invalid(new ValidationError(nameof(hit.Title), "That search result cannot be added."));
        }

        // A combined search can return a series while the movies page is open. Store it as that kind.
        kind = hit.Kind;

        var existing = await items.FindOneAsync(new SearchOptions<WatchItem>
        {
            Query = item => item.OwnerUserId == ownerUserId
                && item.Kind == kind
                && item.CatalogSource == hit.Source
                && item.ExternalId == hit.ExternalId,
            CancellationToken = cancellationToken,
        });

        if (existing is not null)
        {
            return Result<CatalogAdd>.Success(new CatalogAdd(ToDto(existing, Today()), AlreadyListed: true));
        }

        var fresh = await catalog.GetAsync(kind, hit.Source, hit.ExternalId, cancellationToken);
        var chosen = fresh.IsSuccess ? fresh.Value : hit;

        var now = time.GetUtcNow().UtcDateTime;
        var created = await items.InsertAsync(new WatchItem
        {
            Kind = kind,
            Title = CatalogText.Title(chosen.Title),
            Overview = chosen.Overview,
            ReleaseDate = chosen.ReleaseDate,
            LinkUrl = chosen.LinkUrl,
            ImageUrl = chosen.ImageUrl,
            CatalogSource = chosen.Source,
            ExternalId = chosen.ExternalId,
            OwnerUserId = ownerUserId,
            CreatedAt = now,
            UpdatedAt = now,
        });

        logger.LogInformation(
            "Added {Kind} '{Title}' from {Source} {ExternalId}",
            kind,
            created.Title,
            chosen.Source,
            chosen.ExternalId);

        await releases.NotifyDueAsync(cancellationToken);

        var saved = await FindOwnedAsync(created.Id, cancellationToken);
        var dto = saved.IsSuccessStatus() ? ToDto(saved.Value, Today()) : ToDto(created, Today());
        return Result<CatalogAdd>.Success(new CatalogAdd(dto, AlreadyListed: false));
    }

    public async Task<Result<WatchItemDto>> UpdateAsync(
        int id,
        WatchItemForm form,
        CancellationToken cancellationToken = default)
    {
        var found = await FindOwnedAsync(id, cancellationToken);
        if (!found.IsSuccessStatus())
        {
            return found.To<WatchItemDto>();
        }

        var validated = Validate(form);
        if (!validated.IsSuccessStatus())
        {
            return validated.To<WatchItemDto>();
        }

        var item = found.Value;
        bool dateChanged = item.ReleaseDate != form.ReleaseDate;

        item.Title = form.Title.Trim();
        item.Notes = BlankToNull(form.Notes);
        item.ReleaseDate = form.ReleaseDate;
        item.LinkUrl = NormalizeUrl(form.LinkUrl);
        item.ImageUrl = NormalizeUrl(form.ImageUrl);
        item.UpdatedAt = time.GetUtcNow().UtcDateTime;

        if (dateChanged)
        {
            // The previous notice described a date that is no longer true.
            item.ReleaseNotifiedAt = null;
            await notifications.DeleteAsync(
                notice => notice.WatchItemId == item.Id,
                new ContextOptions { CancellationToken = cancellationToken });
        }

        await items.UpdateAsync(item);
        await releases.NotifyDueAsync(cancellationToken);

        var saved = await FindOwnedAsync(id, cancellationToken);
        return saved.IsSuccessStatus()
            ? Result<WatchItemDto>.Success(ToDto(saved.Value, Today()))
            : Result<WatchItemDto>.Success(ToDto(item, Today()));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var found = await FindOwnedAsync(id, cancellationToken);
        if (!found.IsSuccessStatus())
        {
            return found.ToVoid();
        }

        await items.DeleteAsync(found.Value);
        logger.LogInformation("Removed watch item {Id}", id);
        return Result.Success();
    }

    private async Task<Result<WatchItem>> FindOwnedAsync(int id, CancellationToken cancellationToken)
    {
        if (ResolveOwner() is not string ownerUserId)
        {
            return Result<WatchItem>.Unauthorized();
        }

        var item = await items.FindOneAsync(new SearchOptions<WatchItem>
        {
            Query = candidate => candidate.Id == id && candidate.OwnerUserId == ownerUserId,
            CancellationToken = cancellationToken,
        });

        return item is null
            ? Result<WatchItem>.NotFound("That title is no longer on your list.")
            : Result<WatchItem>.Success(item);
    }

    private static Result Validate(WatchItemForm form)
    {
        if (string.IsNullOrWhiteSpace(form.Title))
        {
            return Result.Invalid(new ValidationError(nameof(form.Title), "Give it a title."));
        }

        return form.Title.Trim().Length > Constants.MaxLengths.Title
            ? Result.Invalid(new ValidationError(nameof(form.Title), "That title is too long."))
            : form.Notes is { Length: > Constants.MaxLengths.Notes }
            ? Result.Invalid(new ValidationError(nameof(form.Notes), "Those notes are too long."))
            : !IsAcceptableUrl(form.LinkUrl)
            ? Result.Invalid(new ValidationError(nameof(form.LinkUrl), "The link has to be an http or https URL."))
            : !IsAcceptableUrl(form.ImageUrl)
            ? Result.Invalid(new ValidationError(nameof(form.ImageUrl), "The image has to be an http or https URL."))
            : Result.Success();
    }

    private static bool IsAcceptableUrl(string? value) => string.IsNullOrWhiteSpace(value) || (Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https");

    private static string? NormalizeUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private string? ResolveOwner() => userContext.GetCurrentUserId();

    private DateOnly Today() => DateOnly.FromDateTime(time.GetLocalNow().DateTime);

    private static WatchItemDto ToDto(WatchItem item, DateOnly today) => new(
        item.Id,
        item.Kind,
        item.Title,
        item.Notes,
        item.Overview,
        item.ReleaseDate,
        item.LinkUrl,
        item.ImageUrl,
        item.CatalogSource,
        item.ExternalId,
        item.CreatedAt,
        item.UpdatedAt,
        ReleaseText.IsReleased(item.ReleaseDate, today));

    /// <summary>
    /// Coming-up titles first (soonest at the top), then titles with no date, then ones already out.
    /// </summary>
    private static int Rank(WatchItemDto item, DateOnly today) => item.ReleaseDate is null
            ? 1_000_000
            : item.ReleaseDate.Value >= today
            ? item.ReleaseDate.Value.DayNumber
            : 2_000_000 - item.ReleaseDate.Value.DayNumber;
}