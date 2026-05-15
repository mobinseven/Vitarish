namespace Vitarish.Contents.Domain.Dto;

public record ContentRevisionWriteDto : EntityWriteDto
{
    public Guid ContentId { get; set; }
    public int Version { get; set; }
    public string Snapshot { get; set; } = string.Empty;
}
