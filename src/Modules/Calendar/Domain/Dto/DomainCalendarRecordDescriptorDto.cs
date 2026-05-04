namespace Vitarish.Calendar.Domain.Dto;

public record DomainCalendarRecordDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CalendarObjectCategory? Category { get; set; }

    [JsonPropertyName("visibility")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CalendarObjectVisibility? Visibility { get; set; }

    [JsonPropertyName("tenantEntityId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TenantEntityId { get; set; }

    [JsonPropertyName("providerId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ProviderId { get; set; }

    [JsonPropertyName("subjectId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SubjectId { get; set; }

    [JsonPropertyName("businessStatus")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BusinessStatus { get; set; }
}
