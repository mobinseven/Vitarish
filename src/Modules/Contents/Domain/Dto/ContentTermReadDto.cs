namespace Vitarish.Contents.Domain.Dto;

public record ContentTermReadDto : EntityReadDto
{
    public Guid ContentId { get; set; }
    public Guid TermId { get; set; }
    public ContentTermRole Role { get; set; }
}
