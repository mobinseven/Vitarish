namespace Vitarish.Calendar.Domain.Dto;

public record CalendarAccountDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("localEntityType")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CalendarEntityType? LocalEntityType { get; set; }

    [JsonPropertyName("localEntityId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LocalEntityId { get; set; }

    [JsonPropertyName("isActive")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsActive { get; set; }

    [JsonPropertyName("syncStatus")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SyncStatus? SyncStatus { get; set; }
}
