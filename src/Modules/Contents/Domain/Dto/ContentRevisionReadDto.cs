namespace Vitarish.Contents.Domain.Dto;

public record ContentRevisionReadDto : EntityReadDto
{
    public Guid ContentId { get; set; }
    public int Version { get; set; }
    public string Snapshot { get; set; } = string.Empty;
}
