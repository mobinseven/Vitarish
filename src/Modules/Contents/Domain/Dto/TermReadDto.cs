namespace Vitarish.Contents.Domain.Dto;

public record TermReadDto : EntityReadDto
{
    public TermKind Kind { get; set; }
    public Guid? ParentId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
