namespace Vitarish.Appointments.Domain.Dto;

public record BookableShiftSlot(ShiftReadDto Shift, ShiftReservationReadDto? Occurrence);
