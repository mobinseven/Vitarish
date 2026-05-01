namespace Vitarish.Appointments.Domain.Dto;

public record AppointmentSessionDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AppointmentSessionStatus? Status { get; set; }

    [JsonPropertyName("dateFrom")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? DateFrom { get; set; }

    [JsonPropertyName("dateTo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTimeOffset? DateTo { get; set; }

    [JsonPropertyName("sortBy")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AppointmentSessionSortBy? SortBy { get; set; }

    [JsonPropertyName("descending")]
    public bool Descending { get; set; } = true;
}
