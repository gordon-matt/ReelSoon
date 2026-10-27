using Heroicsoft.NetForge.Abstractions.Navigation;

namespace ReelSoon.Infrastructure;

/// <summary>Watch-list links and their counts. Search stays on the list the user is already viewing.</summary>
public sealed class ReelSoonNavigation(IWatchItemService watchItems) : IShellNavigation
{
    private const string BADGE_TONE = "nf-badge-muted";

    public string? SearchUrlFormat => "/?q={0}";

    public string? FormatSearchUrl(string? term, Uri currentUri)
    {
        string segment = currentUri.AbsolutePath.Trim('/');
        string route = MediaKinds.TryFromRoute(segment, out var kind) ? kind.Route() : MediaKind.Movie.Route();

        return string.IsNullOrWhiteSpace(term)
            ? route
            : $"{route}?q={Uri.EscapeDataString(term.Trim())}";
    }

    public async Task<IReadOnlyList<ForgeNavSection>> GetSectionsAsync(CancellationToken cancellationToken = default)
    {
        WatchListCounts? counts = null;
        var result = await watchItems.GetCountsAsync();

        if (result.IsSuccess)
        {
            counts = result.Value;
        }

        return
        [
            new ForgeNavSection
            {
                Heading = "Watch lists",
                Items =
                [
                    .. MediaKinds.All.Select(kind => new ForgeNavItem
                    {
                        Title = kind.Label(),
                        Href = kind.Route(),
                        Icon = kind.Icon(),
                        Badge = counts?.For(kind).ToString(),
                        BadgeCssClass = BADGE_TONE,
                    }),
                ],
            },
        ];
    }
}