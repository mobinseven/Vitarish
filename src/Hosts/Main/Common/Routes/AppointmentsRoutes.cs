namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Appointments
    {
        public const string UserReservations = "/user/appointments/reservations";
        public const string UserReservationTemplate = "/user/appointments/reservations/{reservationId:guid}";
        public const string UserShifts = "/user/appointments/shifts";
        public const string UserShiftTemplate = "/user/appointments/shifts/{shiftId:guid}";

        public const string OwnerReservations = "/owner/appointments/reservations";
        public const string OwnerReservationTemplate = "/owner/appointments/reservations/{reservationId:guid}";
        public const string OwnerShifts = "/owner/appointments/shifts";
        public const string OwnerShiftCreate = "/owner/appointments/shifts/new";
        public const string OwnerShiftCreateTemplate = "/owner/appointments/shifts/new/{dayOfWeek:int}";
        public const string OwnerShiftTemplate = "/owner/appointments/shifts/{shiftId:guid}";
        public const string OwnerShiftEditTemplate = "/owner/appointments/shifts/{shiftId:guid}/edit";
        public const string OwnerShiftOccurrenceTemplate = "/owner/appointments/shifts/{shiftId:guid}/occurrence";

        public static string Reservation(Guid reservationId) => $"{UserReservations}/{GuidValue(reservationId)}";
        public static string Shift(Guid shiftId) => $"{UserShifts}/{GuidValue(shiftId)}";
        public static string AdminReservation(Guid reservationId) => $"{OwnerReservations}/{GuidValue(reservationId)}";
        public static string AdminShift(Guid shiftId) => $"{OwnerShifts}/{GuidValue(shiftId)}";
        public static string AdminShiftOccurrence(Guid shiftId) => $"{AdminShift(shiftId)}/occurrence";
        public static string AdminShiftEdit(Guid shiftId) => $"{AdminShift(shiftId)}/edit";
        public static string AdminShiftCreateForDay(int? dayOfWeek = null) =>
            dayOfWeek is null
                ? OwnerShiftCreate
                : $"{OwnerShiftCreate}/{IntValue(dayOfWeek.Value)}";
    }
}
