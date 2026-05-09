using Vitarish.Requests.Application.Abstractions;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;
using Vitarish.Commerce.Infrastructure.Data;

namespace Vitarish.Commerce.Infrastructure.SearchProviders;

/// <summary>
/// Exposes Commerce published products for full-text indexing via the Search module.
/// </summary>
internal sealed class CommerceProductsSearchProvider(
    CommerceDbContext dbContext,
    IRequestContext requestContext) : ISearchIndexProvider
{
    public string IndexName => "commerce.products";

    public SearchExactMatchPolicy ExactMatchPolicy => new()
    {
        IsExactMatchFirst = true,
        FieldNames = ["title.keyword"],
        PriorityTier = SearchPriorityTier.High
    };

    public async Task<IReadOnlyList<SearchDocument>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
    {
        string tenantId = !string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? requestContext.TenantId
            : throw new InvalidOperationException("Tenant ID must be set before indexing search documents.");

        var rows = await dbContext.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Published)
            .Select(p => new { p.Id, p.Title, p.Description, p.Handle })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(p => new SearchDocument
        {
            Id = p.Id.ToString(),
            Title = p.Title,
            Body = p.Description,
            Type = "product",
            TenantId = tenantId,
            NavigationUrl = $"/shop/products/{p.Handle}"
        })];
    }
}
