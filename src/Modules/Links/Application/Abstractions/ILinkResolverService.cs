namespace Vitarish.Links.Application.Abstractions;

/// <summary>
/// Resolves a short link by (domain, slug) and returns the appropriate outcome.
/// This is the hot path for public-facing redirect requests.
/// </summary>
public interface ILinkResolverService
{
    Task<LinkResolutionResult> ResolveAsync(string domain, string slug, CancellationToken cancellationToken = default);
}
