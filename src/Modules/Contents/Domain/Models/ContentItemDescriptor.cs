namespace Vitarish.Contents.Domain.Models;

[EntityDescriptorFor(typeof(ContentItem))]
public record ContentItemDescriptor : EntityDescriptor
{
    public ContentItemType? Type { get; init; }
    public ContentStatus? Status { get; init; }
    public string? Slug { get; init; }
    public string? CanonicalPath { get; init; }
    public Guid? AuthorId { get; init; }
    public string? Locale { get; init; }
    public DateTime? PublishedFrom { get; init; }
    public DateTime? PublishedTo { get; init; }
    public Guid? TermId { get; init; }
    public IReadOnlyList<Guid>? TermIds { get; init; }
}
