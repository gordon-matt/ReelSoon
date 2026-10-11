namespace ReelSoon;

/// <summary>Shared wording for a release date so the list, the drawer and notifications agree.</summary>
public static class ReleaseText
{
    public static bool IsReleased(DateOnly? date, DateOnly today) =>
        date is not null && date.Value <= today;

    public static string Status(DateOnly? date, DateOnly today) =>
        date is null ? "No date" : date.Value > today ? "Coming up" : "Released";

    public static string Relative(DateOnly? date, DateOnly today)
    {
        if (date is null)
        {
            return "No release date";
        }

        int days = date.Value.DayNumber - today.DayNumber;

        return days switch
        {
            0 => "Today",
            1 => "Tomorrow",
            -1 => "Yesterday",
            > 1 => $"In {days} days",
            _ => $"{-days} days ago",
        };
    }

    public static string Long(DateOnly date) =>
        date.ToString("d MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
}
