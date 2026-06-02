using Vitarish.Prescription.Application.Abstractions;

namespace Vitarish.Prescription.Infrastructure.Providers;

/// <summary>
/// Default implementation of the provider registry.
/// Resolves adapters by provider key from the DI-registered collection.
/// </summary>
public sealed class InsuranceProviderRegistry : IInsuranceProviderRegistry
{
    private readonly Dictionary<string, IInsuranceProviderAdapter> _adapters;
    private readonly IReadOnlyCollection<string> _registeredProviderKeys;

    public InsuranceProviderRegistry(IEnumerable<IInsuranceProviderAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(a => a.ProviderKey, StringComparer.OrdinalIgnoreCase);
        _registeredProviderKeys = _adapters.Keys.Order(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IInsuranceProviderAdapter? GetAdapter(string providerKey) => string.IsNullOrWhiteSpace(providerKey) ? null : _adapters.GetValueOrDefault(providerKey.Trim());

    public IReadOnlyCollection<string> GetRegisteredProviderKeys() => _registeredProviderKeys;

    public bool IsProviderAvailable(string providerKey) => !string.IsNullOrWhiteSpace(providerKey) && _adapters.ContainsKey(providerKey.Trim());
}
