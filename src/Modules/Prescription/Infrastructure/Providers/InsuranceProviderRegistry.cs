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
        if (string.IsNullOrWhiteSpace(providerKey))
            return null;

        return _adapters.GetValueOrDefault(providerKey.Trim());
    }

    public IReadOnlyCollection<string> GetRegisteredProviderKeys()
    {
        return _adapters.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public bool IsProviderAvailable(string providerKey)
    {
        return !string.IsNullOrWhiteSpace(providerKey) && _adapters.ContainsKey(providerKey.Trim());
    }
}
