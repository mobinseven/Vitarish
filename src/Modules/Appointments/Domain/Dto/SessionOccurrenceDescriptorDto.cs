namespace Vitarish.Appointments.Domain.Dto;

public record SessionOccurrenceDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("sessionId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? SessionId { get; set; }

    [JsonPropertyName("dateFrom")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? DateFrom { get; set; }

    [JsonPropertyName("dateTo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? DateTo { get; set; }

    [JsonPropertyName("isCancelled")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsCancelled { get; set; }
}
