namespace Vitarish.Appointments.Contracts.Dto;

public record BookableShiftSlot(ShiftReadDto Shift, ShiftReservationReadDto? Occurrence);
