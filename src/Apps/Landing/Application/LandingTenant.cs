namespace Vitarish.Landing.Application;

public static class LandingTenant
{
    public static readonly Guid OwnerId = new("d47c76ed-dd02-4e0c-8215-a9e30bcc5e79");
    public static readonly Guid TenantId = new("649f45f8-7c24-4a49-a5a4-cf1b3d5c532c");
    public static readonly Guid PrimaryIndicatorId = new("fb0f6350-83d8-48b0-baa6-5a5dc2d20489");
    public const string AppId = "vitarish-landing";
    public const string Name = "Vitarish Landing";
}

public sealed class LandingPlatformOptions
{
    public string LandingUrl { get; set; } = string.Empty;
}

