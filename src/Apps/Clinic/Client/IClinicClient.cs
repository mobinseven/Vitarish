using Vitarish.Clinic.Contracts.Responses;
using Vitarish.Clinic.Contracts.Results;
namespace Vitarish.Clinic.Client;
public interface IClinicClient
{
    Task<ClinicResult<IReadOnlyList<PatientViewDto>>> ListPatientsAsync(PatientListRequest request, CancellationToken cancellationToken);
    Task<ClinicResult<int>> CountPatientsAsync(PatientCountRequest request, CancellationToken cancellationToken);
    Task<ClinicResult<IReadOnlyList<PatientViewDto>>> SearchPatientsAsync(PatientSearchRequest request, CancellationToken cancellationToken);
    Task<ClinicResult<PatientViewDto>> LookupPatientByIdentificationCodeAsync(PatientIdentificationLookupRequest request, CancellationToken cancellationToken);
    Task<ClinicResult<PatientViewDto>> FindOrCreatePatientAsync(PatientFindOrCreateRequest request, CancellationToken cancellationToken);
    Task<ClinicResult<PatientViewDto>> GetPatientDetailsAsync(Guid patientId, CancellationToken cancellationToken);
}
