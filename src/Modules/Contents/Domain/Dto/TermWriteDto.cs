using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Dto;

public record TermWriteDto : EntityWriteDto
{
    public TermKind Kind { get; set; } = TermKind.Tag;
    public Guid? ParentId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
