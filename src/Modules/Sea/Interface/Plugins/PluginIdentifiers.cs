namespace Vitarish.Sea.Interface.Plugins;

/// <summary>
/// Known plugin identifiers exposed by the platform.
/// </summary>
public static class PluginIdentifiers
{
    public const string Clinic = nameof(Clinic);
    public const string Gym = nameof(Gym);
    public static PluginIdentifier[] List =>
        [
            new PluginIdentifier(Clinic, "fa-solid fa-clinic-medical"),
            new PluginIdentifier(Gym, "fa-solid fa-dumbbell")
        ];
}

public record PluginIdentifier(string Title, string Icon);
