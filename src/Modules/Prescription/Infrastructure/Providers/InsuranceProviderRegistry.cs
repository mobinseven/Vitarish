using Vitarish.Prescription.Application.Abstractions;

namespace Vitarish.Prescription.Infrastructure.Providers;

/// <summary>
/// Default implementation of the provider registry.
/// Resolves adapters by provider key from the DI-registered collection.
/// </summary>
public sealed class InsuranceProviderRegistry : IInsuranceProviderRegistry
{
    private readonly Dictionary<string, IInsuranceProviderAdapter> _adapters;

    public InsuranceProviderRegistry(IEnumerable<IInsuranceProviderAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(a => a.ProviderKey, StringComparer.OrdinalIgnoreCase);
    }

    public IInsuranceProviderAdapter? GetAdapter(string providerKey)
    {
        return _adapters.GetValueOrDefault(providerKey);
    }

    public IReadOnlyCollection<string> GetRegisteredProviderKeys()
    {
        return _adapters.Keys;
    }

    public bool IsProviderAvailable(string providerKey)
    {
        return _adapters.ContainsKey(providerKey);
    }
}
