namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Prescriptions
    {
        public const string OwnerPrescriptions = "/owner/clinic/prescriptions";
        public const string OwnerPrescriptionTemplate = "/owner/clinic/prescriptions/{prescriptionId:guid}";
        public const string OwnerPrescriptionCreate = "/owner/clinic/prescriptions/new";
        public const string OwnerPrescriptionCreateTemplate = "/owner/clinic/prescriptions/new/{patientId:guid}";

        public static string OwnerPrescription(Guid prescriptionId) => $"{OwnerPrescriptions}/{GuidValue(prescriptionId)}";
        public static string OwnerPrescriptionCreateForPatient(Guid patientId) => $"{OwnerPrescriptionCreate}/{GuidValue(patientId)}";
    }
}
