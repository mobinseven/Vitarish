namespace Vitarish.Sea.Interface.Plugins;

/// <summary>
/// Known plugin identifiers exposed by the platform.
/// </summary>
public static class SeaPluginIdentifiers
{
    public const string Clinic = nameof(Clinic);
    public const string Gym = nameof(Gym);
    public static string[] List => [Clinic, Gym];
}
