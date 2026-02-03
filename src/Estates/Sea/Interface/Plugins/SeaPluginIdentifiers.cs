namespace Vitarish.Sea.Interface.Plugins;

/// <summary>
/// Known plugin identifiers exposed by the platform.
/// </summary>
public static class SeaPluginIdentifiers
{
    public const string Clinic = nameof(Clinic);
    public const string Gym = nameof(Gym);
    public static PluginIdentifier[] List => [new PluginIdentifier(Clinic, "fa-regular fa-clinic-medical"), new PluginIdentifier(Gym, "fa-regular fa-dumbbell")];
}

public record PluginIdentifier(string Title, string Icon);
