namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Medical
    {
        public const string Dashboard = "/medical/admin";
        public const string CheckIn = "/medical/check-in";
        public const string Patients = "/medical/patients";
        public const string PatientNew = "/medical/patients/new";
        public const string PatientTemplate = "/medical/patients/{patientId:guid}";
        public const string PatientEditTemplate = "/medical/patients/{patientId:guid}/edit";
        public const string PatientProcedureTemplate = "/medical/patients/{patientId:guid}/procedures/{procedureId:guid}";
        public const string PatientEventTemplate = "/medical/patients/{patientId:guid}/procedures/{procedureId:guid}/events/{eventId:guid}";

        public static string Patient(Guid patientId) => $"{Patients}/{GuidValue(patientId)}";
        public static string PatientEdit(Guid patientId) => $"{Patient(patientId)}/edit";
        public static string PatientProcedure(Guid patientId, Guid procedureId) =>
            $"{Patient(patientId)}/procedures/{GuidValue(procedureId)}";
        public static string PatientEvent(Guid patientId, Guid procedureId, Guid eventId) =>
            $"{PatientProcedure(patientId, procedureId)}/events/{GuidValue(eventId)}";
    }
}
