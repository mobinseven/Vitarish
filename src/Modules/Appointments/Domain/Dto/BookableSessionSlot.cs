namespace Vitarish.Appointments.Domain.Dto;

public record BookableSessionSlot(SessionReadDto Session, SessionReservationReadDto? Occurrence);
