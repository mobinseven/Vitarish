using Vitarish.Modules.Base.Core.Platform.Errors;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;

internal sealed record TaminProviderJsonResult(string OperationName, string RawJson, IntegrationError? Error = null)
{
    public bool IsSuccess => Error is null;
    public static TaminProviderJsonResult Success(string operationName, string rawJson) => new(operationName, rawJson);
    public static TaminProviderJsonResult Failure(string operationName, IntegrationError error) => new(operationName, string.Empty, error);
}

internal sealed record TaminDoctorScopedOperationRequest(Guid DoctorId, Guid ClinicId);
internal sealed record TaminWarningOperationRequest(Guid DoctorId, Guid ClinicId, string PatientNationalId, IReadOnlyList<object> PrescriptionItems);
internal sealed record TaminReferralCountOperationRequest(Guid DoctorId, Guid ClinicId, string PatientNationalCode, string TaminDoctorId);
internal sealed record TaminReferralFeedbackOperationRequest(Guid DoctorId, Guid ClinicId, string ReferralId, string Message);
internal sealed record TaminNurseTodoListOperationRequest(Guid DoctorId, Guid ClinicId, string SiamId, string PatientNationalCode);
internal sealed record TaminNurseActionOperationRequest(Guid DoctorId, Guid ClinicId, string SiamId, string NurseNationalCode, IReadOnlyList<long> PrescriptionDetailIds);
internal sealed record TaminHospitalizationCreateOperationRequest(Guid DoctorId, Guid ClinicId, string ReferralId, string SiamId);
internal sealed record TaminHospitalizationSecretaryListOperationRequest(Guid DoctorId, Guid ClinicId, string SiamId, string SecretaryNationalCode);
