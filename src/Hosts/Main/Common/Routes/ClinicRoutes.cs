namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Clinic
    {
        public const string OwnerDashboard = "/owner/clinic";
        public const string CheckIn = "/owner/clinic/check-in";
        public const string Patients = "/owner/clinic/patients";
        public const string PatientNew = "/owner/clinic/patients/new";
        public const string PatientTemplate = "/owner/clinic/patients/{patientId:guid}";
        public const string PatientEditTemplate = "/owner/clinic/patients/{patientId:guid}/edit";
        public const string PatientProcedureTemplate = "/owner/clinic/patients/{patientId:guid}/procedures/{procedureId:guid}";
        public const string PatientEventTemplate = "/owner/clinic/patients/{patientId:guid}/procedures/{procedureId:guid}/events/{eventId:guid}";

        public const string OwnerSettings = "/owner/clinic/settings";
        public const string OwnerSettingsUsers = "/owner/clinic/settings/users";
        public const string OwnerSettingsStatistics = "/owner/clinic/settings/statistics";
        public const string OwnerReminders = "/owner/clinic/reminders";

        // Owner appointment routes (Appointments module hosted under Clinic)
        public const string OwnerReservations = "/owner/clinic/reservations";
        public const string OwnerReservationTemplate = "/owner/clinic/reservations/{reservationId:guid}";
        public const string OwnerShifts = "/owner/clinic/shifts";
        public const string OwnerShiftCreate = "/owner/clinic/shifts/new";
        public const string OwnerShiftCreateTemplate = "/owner/clinic/shifts/new/{dayOfWeek:int}";
        public const string OwnerShiftTemplate = "/owner/clinic/shifts/{shiftId:guid}";
        public const string OwnerShiftEditTemplate = "/owner/clinic/shifts/{shiftId:guid}/edit";
        public const string OwnerShiftOccurrenceTemplate = "/owner/clinic/shifts/{shiftId:guid}/occurrence";

        // User appointment routes
        public const string UserReservations = "/user/clinic/reservations";
        public const string UserReservationTemplate = "/user/clinic/reservations/{reservationId:guid}";
        public const string UserShifts = "/user/clinic/shifts";
        public const string UserShiftTemplate = "/user/clinic/shifts/{shiftId:guid}";

        public static string Patient(Guid patientId) => $"{Patients}/{GuidValue(patientId)}";
        public static string PatientEdit(Guid patientId) => $"{Patient(patientId)}/edit";
        public static string PatientProcedure(Guid patientId, Guid procedureId) =>
            $"{Patient(patientId)}/procedures/{GuidValue(procedureId)}";
        public static string PatientEvent(Guid patientId, Guid procedureId, Guid eventId) =>
            $"{PatientProcedure(patientId, procedureId)}/events/{GuidValue(eventId)}";

        // Owner appointment builders
        public static string OwnerReservation(Guid reservationId) => $"{OwnerReservations}/{GuidValue(reservationId)}";
        public static string OwnerShift(Guid shiftId) => $"{OwnerShifts}/{GuidValue(shiftId)}";
        public static string OwnerShiftEdit(Guid shiftId) => $"{OwnerShift(shiftId)}/edit";
        public static string OwnerShiftOccurrence(Guid shiftId) => $"{OwnerShift(shiftId)}/occurrence";
        public static string OwnerShiftCreateForDay(int? dayOfWeek = null) =>
            dayOfWeek is null ? OwnerShiftCreate : $"{OwnerShiftCreate}/{IntValue(dayOfWeek.Value)}";

        // User appointment builders
        public static string UserReservation(Guid reservationId) => $"{UserReservations}/{GuidValue(reservationId)}";
        public static string UserShift(Guid shiftId) => $"{UserShifts}/{GuidValue(shiftId)}";

        // Service catalog routes
        public const string OwnerServiceTypes = "/owner/clinic/service-types";
        public const string OwnerServiceTypeTemplate = "/owner/clinic/service-types/{serviceTypeId:guid}";
        public const string OwnerServices = "/owner/clinic/services";
        public const string OwnerServiceTemplate = "/owner/clinic/services/{serviceId:guid}";
        public const string OwnerServiceProviders = "/owner/clinic/service-providers";
        public const string OwnerServiceProviderTemplate = "/owner/clinic/service-providers/{serviceProviderId:guid}";

        // Service catalog builders
        public static string OwnerServiceType(Guid serviceTypeId) => $"{OwnerServiceTypes}/{GuidValue(serviceTypeId)}";
        public static string OwnerService(Guid serviceId) => $"{OwnerServices}/{GuidValue(serviceId)}";
        public static string OwnerServiceProvider(Guid serviceProviderId) => $"{OwnerServiceProviders}/{GuidValue(serviceProviderId)}";
    }
}
