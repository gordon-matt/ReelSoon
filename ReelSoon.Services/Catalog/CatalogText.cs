namespace ReelSoon.Services.Catalog;

internal static class CatalogText
{
    public static string Title(string? value)
    {
        string trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= Constants.MaxLengths.Title
            ? trimmed
            : trimmed[..Constants.MaxLengths.Title];
    }

    public static string? Clip(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    public static string? HttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        if (trimmed.Length > Constants.MaxLengths.Url)
        {
            return null;
        }

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https"
            ? trimmed
            : null;
    }

    public static DateOnly? Date(string? value) =>
        DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out var date)
            ? date
            : null;

    public static DateOnly? UnixDate(long? seconds)
    {
        if (seconds is not > 0)
        {
            return null;
        }

        var instant = DateTimeOffset.FromUnixTimeSeconds(seconds.Value);
        return DateOnly.FromDateTime(instant.UtcDateTime);
    }
}
