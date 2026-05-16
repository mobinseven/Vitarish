namespace Vitarish.Contents.Domain.Dto;

public record ContentTermWriteDto : EntityWriteDto
{
    public Guid ContentId { get; set; }
    public Guid TermId { get; set; }
    public ContentTermRole Role { get; set; } = ContentTermRole.Classification;
}
