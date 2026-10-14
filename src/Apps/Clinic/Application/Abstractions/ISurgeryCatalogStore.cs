using Vitarish.Clinic.Domain.Surgery;

namespace Vitarish.Clinic.Application.Abstractions;

/// <summary>
/// The current clinic's surgery types. Mutations throw <see cref="ArgumentException"/> for invalid input and
/// <see cref="InvalidOperationException"/> for concurrency conflicts; they return null/false when the type does not exist.
/// </summary>
public interface ISurgeryCatalogStore
{
    Task<IReadOnlyList<SurgeryCatalog>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Finds a type by its id, or the clinic's default type by <see cref="SurgeryCatalogDefaults.LegacyGeneralSurgeryId"/>.</summary>
    Task<SurgeryCatalog?> FindAsync(string catalogId, CancellationToken cancellationToken);

    Task<SurgeryCatalog> CreateAsync(SurgeryCatalogWriteModel catalog, CancellationToken cancellationToken);
    Task<SurgeryCatalog?> UpdateAsync(Guid id, SurgeryCatalogWriteModel catalog, Guid concurrencyToken, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, Guid concurrencyToken, CancellationToken cancellationToken);

    /// <summary>
    /// Idempotently creates the clinic's default type from <paramref name="defaults"/> when it does not exist yet,
    /// and returns the stored default type. Concurrent calls create it once.
    /// </summary>
    Task<SurgeryCatalog> EnsureDefaultAsync(SurgeryCatalogWriteModel defaults, CancellationToken cancellationToken);
}
