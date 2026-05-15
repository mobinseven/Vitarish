namespace Vitarish.Contents.Domain.Dto;

public record ContentRelationReadDto : EntityReadDto
{
    public Guid FromContentId { get; set; }
    public Guid ToContentId { get; set; }
    public ContentRelationType RelationType { get; set; }
    public decimal? Weight { get; set; }
}
