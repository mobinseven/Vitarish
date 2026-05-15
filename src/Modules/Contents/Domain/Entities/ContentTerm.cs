namespace Vitarish.Contents.Domain.Entities;

public class ContentTerm : Entity
{
    public Guid ContentId { get; set; }
    public Guid TermId { get; set; }
    public ContentTermRole Role { get; set; } = ContentTermRole.Classification;
}
