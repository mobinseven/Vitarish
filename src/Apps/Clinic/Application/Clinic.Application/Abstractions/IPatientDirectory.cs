using Vitarish.Clinic.Application.Results;
using Vitarish.Clinic.Domain.Models;

namespace Vitarish.Clinic.Application.Abstractions;

public interface IPatientDirectory
{
    Task<PatientView?> FindByIdentificationCodeAsync(string identificationCode, CancellationToken cancellationToken);

    Task<PatientCreationResult> CreateAsync(PatientWriteDto patient, CancellationToken cancellationToken);
}
