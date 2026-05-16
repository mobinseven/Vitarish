namespace Vitarish.Links.Infrastructure.Services;

/// <summary>
/// Resolves a short link by looking up (domain, slug) globally.
/// Does not require a tenant context; operates across all tenants.
/// </summary>
[RegisterTransient]
internal sealed class LinkResolverService(LinksDbContext context) : ILinkResolverService
{
    public async Task<LinkResolutionResult> ResolveAsync(string domain, string slug, CancellationToken cancellationToken = default)
    {
        Link? link = await context.Links
            .AsNoTracking()
            .Where(l => l.Domain == domain && l.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (link is null)
        {
            return new LinkResolutionResult.NotFound();
        }

        if (link.Status == LinkStatus.Disabled)
        {
            return new LinkResolutionResult.Disabled();
        }

        if (link.ExpiresAt.HasValue && link.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            return new LinkResolutionResult.Expired();
        }

        return new LinkResolutionResult.Active(link.DestinationUrl);
    }
}
