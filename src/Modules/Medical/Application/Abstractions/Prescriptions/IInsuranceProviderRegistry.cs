namespace Vitarish.Medical.Application.Abstractions.Prescriptions;

/// <summary>
/// Resolves the correct insurance provider adapter by provider key.
/// </summary>
public interface IInsuranceProviderRegistry
{
    /// <summary>Gets an adapter by provider key, or null if not registered.</summary>
    IInsuranceProviderAdapter? GetAdapter(string providerKey);

    /// <summary>Gets all registered provider keys.</summary>
    IReadOnlyCollection<string> GetRegisteredProviderKeys();

    /// <summary>Checks if a provider is registered and active.</summary>
    bool IsProviderAvailable(string providerKey);
}
