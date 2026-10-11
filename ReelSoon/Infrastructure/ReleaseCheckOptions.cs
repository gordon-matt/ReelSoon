namespace ReelSoon.Infrastructure;

public sealed class ReleaseCheckOptions
{
    public const string SectionName = "Releases";

    /// <summary>How often to look for titles whose release date has arrived.</summary>
    public int CheckIntervalSeconds { get; set; } = 60;
}
