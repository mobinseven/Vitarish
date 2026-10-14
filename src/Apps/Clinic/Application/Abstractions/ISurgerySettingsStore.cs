using Vitarish.Clinic.Domain.Surgery;

namespace Vitarish.Clinic.Application.Abstractions;

/// <summary>
/// The current clinic's surgery settings. Mutations throw <see cref="ArgumentException"/> for invalid input,
/// <see cref="InvalidOperationException"/> for concurrency conflicts, and <see cref="SurgeryPlanningForbiddenException"/>
/// when the caller may not manage surgeries; they return null when the clinic has no settings.
/// </summary>
public interface ISurgerySettingsStore
{
    Task<SurgerySettings?> GetAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Idempotently creates the clinic's settings from <paramref name="defaults"/> when they do not exist yet,
    /// and returns the stored settings unchanged otherwise. Concurrent calls create them once.
    /// </summary>
    Task<SurgerySettings> EnsureDefaultAsync(SurgerySettingsWriteModel defaults, CancellationToken cancellationToken);

    Task<SurgerySettings?> UpdateAsync(SurgerySettingsWriteModel model, Guid concurrencyToken, CancellationToken cancellationToken);
}
