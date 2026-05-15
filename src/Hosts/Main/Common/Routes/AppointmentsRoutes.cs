namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Appointments
    {
        public const string Reservations = "/appointments/reservations";
        public const string ReservationTemplate = "/appointments/reservations/{reservationId:guid}";
        public const string Shifts = "/appointments/shifts";
        public const string ShiftTemplate = "/appointments/shifts/{shiftId:guid}";

        public const string AdminReservations = "/admin/appointments/reservations";
        public const string AdminReservationTemplate = "/admin/appointments/reservations/{reservationId:guid}";
        public const string AdminShifts = "/admin/appointments/shifts";
        public const string AdminShiftCreate = "/admin/appointments/shifts/new";
        public const string AdminShiftCreateTemplate = "/admin/appointments/shifts/new/{dayOfWeek:int}";
        public const string AdminShiftTemplate = "/admin/appointments/shifts/{shiftId:guid}";
        public const string AdminShiftEditTemplate = "/admin/appointments/shifts/{shiftId:guid}/edit";
        public const string AdminShiftOccurrenceTemplate = "/admin/appointments/shifts/{shiftId:guid}/occurrence";

        public static string Reservation(Guid reservationId) => $"{Reservations}/{GuidValue(reservationId)}";
        public static string Shift(Guid shiftId) => $"{Shifts}/{GuidValue(shiftId)}";
        public static string AdminReservation(Guid reservationId) => $"{AdminReservations}/{GuidValue(reservationId)}";
        public static string AdminShift(Guid shiftId) => $"{AdminShifts}/{GuidValue(shiftId)}";
        public static string AdminShiftOccurrence(Guid shiftId) => $"{AdminShift(shiftId)}/occurrence";
        public static string AdminShiftEdit(Guid shiftId) => $"{AdminShift(shiftId)}/edit";
        public static string AdminShiftCreateForDay(int? dayOfWeek = null) =>
            dayOfWeek is null
                ? AdminShiftCreate
                : $"{AdminShiftCreate}/{IntValue(dayOfWeek.Value)}";
    }
}
