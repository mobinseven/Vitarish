namespace Vitarish.Links.Infrastructure.Services;

/// <summary>
/// Resolves a short link by looking up (domain, slug) globally.
/// Does not require a tenant context; operates across all tenants.
/// </summary>
[RegisterTransient]
internal sealed class LinkResolverService(LinksDbContext context, TimeProvider timeProvider) : ILinkResolverService
{
    public async Task<LinkResolutionResult> ResolveAsync(string domain, string slug, CancellationToken cancellationToken = default)
    {
        string normalizedDomain = LinkRules.NormalizeDomain(domain);
        string normalizedSlug = slug.Trim();

        Link? link = await context.Links
            .AsNoTracking()
            .Where(l => l.Domain == normalizedDomain && l.Slug == normalizedSlug)
            .FirstOrDefaultAsync(cancellationToken);

        return link is null
            ? new LinkResolutionResult.NotFound()
            : link.Status == LinkStatus.Disabled
            ? new LinkResolutionResult.Disabled()
            : link.ExpiresAt.HasValue && link.ExpiresAt.Value < timeProvider.GetUtcNow()
            ? new LinkResolutionResult.Expired()
            : new LinkResolutionResult.Active(link.DestinationUrl);
    }
}
