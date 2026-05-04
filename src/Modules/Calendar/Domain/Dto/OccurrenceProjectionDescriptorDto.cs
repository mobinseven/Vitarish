namespace Vitarish.Calendar.Domain.Dto;

public record OccurrenceProjectionDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("mappingId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? MappingId { get; set; }

    [JsonPropertyName("rangeStart")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? RangeStart { get; set; }

    [JsonPropertyName("rangeEnd")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? RangeEnd { get; set; }

    [JsonPropertyName("isCancelled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsCancelled { get; set; }
}
