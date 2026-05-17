namespace Vitarish.Links.Domain.Models;

[EntityDescriptorFor(typeof(Link))]
public record LinkDescriptor : EntityDescriptor
{
    public Guid? TenantId { get; init; }
    public string? Domain { get; init; }
    public string? Slug { get; init; }
    public LinkStatus? Status { get; init; }
}
