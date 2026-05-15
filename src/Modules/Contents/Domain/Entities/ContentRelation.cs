namespace Vitarish.Contents.Domain.Entities;

public class ContentRelation : Entity
{
    public Guid FromContentId { get; set; }
    public Guid ToContentId { get; set; }
    public ContentRelationType RelationType { get; set; } = ContentRelationType.RelatedTo;
    public decimal? Weight { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
