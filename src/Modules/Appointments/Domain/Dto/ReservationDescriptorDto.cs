namespace Vitarish.Appointments.Domain.Dto;

public record ReservationDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReservationStatus? Status { get; set; }

    [JsonPropertyName("sessionOccurrenceId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? SessionOccurrenceId { get; set; }

    [JsonPropertyName("reservingPartyId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReservingPartyId { get; set; }

    [JsonPropertyName("isHold")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsHold { get; set; }
}
