using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Contracts.Dto;

public record ReservationDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReservationStatus? Status { get; set; }

    [JsonPropertyName("shiftId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ShiftId { get; set; }

    [JsonPropertyName("dateFrom")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? DateFrom { get; set; }

    [JsonPropertyName("dateTo")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? DateTo { get; set; }

    [JsonPropertyName("reservingPartyId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ReservingPartyId { get; set; }

    [JsonPropertyName("isHold")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsHold { get; set; }
}
