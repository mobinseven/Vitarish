using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Dto;

public record ContentItemReadDto : EntityReadDto
{
    public ContentItemType Type { get; set; }
    public ContentStatus Status { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string CanonicalPath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public IReadOnlyList<ContentBodyBlock> Body { get; set; } = [];
    public Guid AuthorId { get; set; }
    public string Locale { get; set; } = "en";
    public DateTime? PublishedAt { get; set; }
}
