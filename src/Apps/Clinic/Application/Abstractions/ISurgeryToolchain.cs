using Vitarish.Clinic.Domain.Surgery;

namespace Vitarish.Clinic.Application.Abstractions;

public enum SurgeryCreateOperationState
{
    Creating,
    Completed
}

public sealed record SurgeryCreateOperation(
    string Fingerprint,
    Guid SurgeryId,
    SurgeryCreateOperationState State,
    DateTime LeaseExpiresAt);

public sealed class SurgeryOperationUnavailableException(Exception innerException) : Exception(
    "The surgery operation could not be completed because a dependent service is unavailable.",
    innerException);

public interface ISurgeryToolchain
{
    IReadOnlyList<SurgeryCatalog> GetCatalogs();
    Task<IReadOnlyList<SurgeryReadModel>> GetUpcomingAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SurgeryReadModel>> GetRangeAsync(DateOnly dateFrom, DateOnly dateTo, int take, CancellationToken cancellationToken);
    Task<IReadOnlyList<SurgeryReadModel>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken);
    Task<SurgeryReadModel?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<SurgeryReadModel> CreateAsync(SurgeryWriteModel surgery, CancellationToken cancellationToken);
    Task<SurgeryReadModel?> UpdateAsync(SurgeryWriteModel surgery, CancellationToken cancellationToken);
    Task<SurgeryReadModel?> CancelAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken);
}
