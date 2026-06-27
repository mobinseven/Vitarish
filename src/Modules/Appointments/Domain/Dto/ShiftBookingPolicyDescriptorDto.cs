using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Domain.Dto;

public record ShiftBookingPolicyDescriptorDto : EntityDescriptorDto
{
    [JsonPropertyName("shiftId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? ShiftId { get; set; }

    [JsonPropertyName("date")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? Date { get; set; }

    [JsonPropertyName("closed")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Closed { get; set; }
}
