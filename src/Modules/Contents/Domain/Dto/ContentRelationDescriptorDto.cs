using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Dto;

public record ContentRelationDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("fromContentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? FromContentId { get; set; }

    [JsonPropertyName("toContentId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ToContentId { get; set; }

    [JsonPropertyName("relationType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ContentRelationType? RelationType { get; set; }
}
