using System.Text.Json.Serialization;

namespace Vitarish.Appointments.Domain.Dto;

public record BookableShiftSlot(ShiftReadDto Shift, ShiftReservationReadDto? Occurrence)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ServiceName { get; init; }
}
