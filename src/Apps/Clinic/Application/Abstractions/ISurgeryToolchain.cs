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

/// <summary>
/// Surgery planning use cases. Mutations throw <see cref="ArgumentException"/> for invalid input,
/// <see cref="InvalidOperationException"/> for concurrency or status-transition conflicts, and
/// <see cref="SurgeryOperationUnavailableException"/> when a dependency fails; they return null when the surgery does not exist.
/// </summary>
public interface ISurgeryToolchain
{
    Task<IReadOnlyList<SurgeryReadModel>> GetUpcomingAsync(CancellationToken cancellationToken);

    /// <summary>All surgeries, any status, starting in [<paramref name="from"/>, <paramref name="to"/>), ordered by start.</summary>
    Task<IReadOnlyList<SurgeryCalendarEntry>> GetCalendarAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);

    Task<IReadOnlyList<SurgeryReadModel>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken);
    Task<SurgeryCalendarEntry?> GetAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Planned surgeries that overlap the probe and share its location or a team member.</summary>
    Task<IReadOnlyList<SurgeryConflict>> FindConflictsAsync(SurgeryConflictProbe probe, CancellationToken cancellationToken);

    Task<SurgerySuggestions> GetSuggestionsAsync(CancellationToken cancellationToken);
    Task<SurgeryReadModel> CreateAsync(SurgeryWriteModel surgery, CancellationToken cancellationToken);
    Task<SurgeryReadModel?> UpdateAsync(SurgeryWriteModel surgery, CancellationToken cancellationToken);
    Task<SurgeryReadModel?> CompleteAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken);
    Task<SurgeryReadModel?> CancelAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken);
}
