namespace Vitarish.Landing.Application;

public static class LandingTenant
{
    public const string OwnerName = "Vitarish";
    public const string AppId = "vitarish-landing";
    public const string Name = "Vitarish Landing";
}

public sealed class LandingPlatformOptions
{
    public string LandingUrl { get; set; } = string.Empty;
}

