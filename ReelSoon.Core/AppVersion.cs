using System.Reflection;

namespace ReelSoon;

/// <summary>
/// The running assembly's version, plus the UTC time that assembly was written.
/// Useful on a NAS or Docker host to confirm a deploy actually replaced the bits.
/// </summary>
public static class AppVersion
{
    private static readonly Lazy<string> CurrentLazy = new(Resolve);

    public static string Current => CurrentLazy.Value;

    private static string Resolve()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(AppVersion).Assembly;
        string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?.Split('+', 2)[0]
                ?? assembly.GetName().Version?.ToString(3)
                ?? "0.0.0";

        try
        {
            var built = File.GetLastWriteTimeUtc(assembly.Location);
            if (built > DateTime.UnixEpoch)
            {
                return $"{version}+{built:yyyyMMdd.HHmm}";
            }
        }
        catch (IOException)
        {
        }

        return version;
    }
}
