namespace Vitarish.Calendar.Domain.Dto;

public record CalendarObjectMappingDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("calendarAccountId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? CalendarAccountId { get; set; }

    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CalendarObjectCategory? Category { get; set; }

    [JsonPropertyName("iCalendarUid")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ICalendarUid { get; set; }

    [JsonPropertyName("isExternallyManaged")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsExternallyManaged { get; set; }
}
