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

        public static string Patient(Guid patientId) => $"{Patients}/{GuidValue(patientId)}";
        public static string PatientEdit(Guid patientId) => $"{Patient(patientId)}/edit";
        public static string PatientProcedure(Guid patientId, Guid procedureId) =>
            $"{Patient(patientId)}/procedures/{GuidValue(procedureId)}";
        public static string PatientEvent(Guid patientId, Guid procedureId, Guid eventId) =>
            $"{PatientProcedure(patientId, procedureId)}/events/{GuidValue(eventId)}";
    }
}
