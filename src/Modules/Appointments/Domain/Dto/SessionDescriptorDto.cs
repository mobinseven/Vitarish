namespace Vitarish.Appointments.Domain.Dto;

public record SessionDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SessionStatus? Status { get; set; }

    [JsonPropertyName("dateFrom")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? DateFrom { get; set; }

    [JsonPropertyName("dateTo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? DateTo { get; set; }

    [JsonPropertyName("sortBy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SessionSortBy? SortBy { get; set; }

    [JsonPropertyName("descending")]
    public bool Descending { get; set; } = true;
}
