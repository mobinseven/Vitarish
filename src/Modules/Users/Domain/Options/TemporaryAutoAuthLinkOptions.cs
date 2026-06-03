namespace Vitarish.Users.Domain.Options;

public sealed class TemporaryAutoAuthLinkOptions
{
    public const string SectionName = "TemporaryAutoAuthLinks";

    public string HmacSecret { get; init; } = string.Empty;

    public int DefaultLifetimeMinutes { get; init; } = 10;

    public int RetentionDays { get; init; } = 7;

    public IDictionary<string, TemporaryAutoAuthLinkPurposeOptions> Purposes { get; init; }
        = new Dictionary<string, TemporaryAutoAuthLinkPurposeOptions>(StringComparer.Ordinal);
}

public sealed class TemporaryAutoAuthLinkPurposeOptions
{
    public int LifetimeMinutes { get; init; } = 10;

    public string[] AllowedDestinationPathPrefixes { get; init; } = ["/"];
}
