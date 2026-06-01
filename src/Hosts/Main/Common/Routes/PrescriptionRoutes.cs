namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Prescriptions
    {
        public const string OwnerPrescriptions = "/owner/clinic/prescriptions";
        public const string OwnerPrescriptionTemplate = "/owner/clinic/prescriptions/{prescriptionId:guid}";
        public const string OwnerPrescriptionCreate = "/owner/clinic/prescriptions/new";
        public const string OwnerPrescriptionCreateTemplate = "/owner/clinic/prescriptions/new/{patientId:guid}";

        // Admin routes
        public const string OwnerPrescriptionOperationalDashboard = "/owner/clinic/prescriptions/admin/operational";
        public const string OwnerPrescriptionReferenceDataAdmin = "/owner/clinic/prescriptions/admin/reference-data";
        public const string OwnerPrescriptionCredentialSetup = "/owner/clinic/prescriptions/admin/credentials";

        // Support routes
        public const string SupportPrescriptionReconciliationDesk = "/support/prescriptions/reconciliation";

        // Platform admin routes
        public const string PlatformAdminPrescriptionProviders = "/admin/prescriptions/providers";

        public static string OwnerPrescription(Guid prescriptionId) => $"{OwnerPrescriptions}/{GuidValue(prescriptionId)}";
        public static string OwnerPrescriptionCreateForPatient(Guid patientId) => $"{OwnerPrescriptionCreate}/{GuidValue(patientId)}";
    }
}
