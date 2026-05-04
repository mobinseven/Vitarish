namespace Vitarish.Calendar.Domain.Dto;

public record AvailabilityPolicyDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("calendarAccountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CalendarAccountId { get; set; }

    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CalendarObjectCategory? Category { get; set; }
}
