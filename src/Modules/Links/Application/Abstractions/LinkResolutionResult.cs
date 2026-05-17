namespace Vitarish.Links.Application.Abstractions;

/// <summary>
/// Discriminated-union result returned by <see cref="ILinkResolverService"/>.
/// </summary>
public abstract record LinkResolutionResult
{
    /// <summary>Link found, active, and not expired. Visitor should be redirected to <see cref="DestinationUrl"/>.</summary>
    public sealed record Active(string DestinationUrl) : LinkResolutionResult;

    /// <summary>Link exists but has been manually disabled.</summary>
    public sealed record Disabled : LinkResolutionResult;

    /// <summary>Link exists but its expiry time has passed.</summary>
    public sealed record Expired : LinkResolutionResult;

    /// <summary>No link matching (domain, slug) was found.</summary>
    public sealed record NotFound : LinkResolutionResult;
}
