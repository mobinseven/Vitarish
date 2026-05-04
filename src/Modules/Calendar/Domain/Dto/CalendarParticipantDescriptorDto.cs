namespace Vitarish.Calendar.Domain.Dto;

public record CalendarParticipantDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("mappingId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? MappingId { get; set; }

    [JsonPropertyName("participantType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ParticipantType? ParticipantType { get; set; }
}
