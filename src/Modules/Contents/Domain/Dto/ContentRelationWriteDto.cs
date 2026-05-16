namespace Vitarish.Contents.Domain.Dto;

public record ContentRelationWriteDto : EntityWriteDto
{
    public Guid FromContentId { get; set; }
    public Guid ToContentId { get; set; }
    public ContentRelationType RelationType { get; set; } = ContentRelationType.RelatedTo;
    public decimal? Weight { get; set; }
}
