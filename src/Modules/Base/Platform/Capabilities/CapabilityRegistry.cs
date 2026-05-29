namespace Vitarish.Modules.Base.Platform.Capabilities;

/// <summary>
/// Centralized registry for capability providers. Modules register their providers here;
/// UI and orchestration layers query it to gate operations.
/// </summary>
/// <typeparam name="TCapability">Enum or flag type defining available capabilities.</typeparam>
public sealed class CapabilityRegistry<TCapability> where TCapability : struct, Enum
{
    private readonly Dictionary<string, ICapabilityProvider<TCapability>> _providers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers a capability provider.</summary>
    public void Register(ICapabilityProvider<TCapability> provider)
    {
        _providers[provider.ProviderKey] = provider;
    }

    /// <summary>Gets a provider by key, or null if not registered.</summary>
    public ICapabilityProvider<TCapability>? GetProvider(string providerKey)
    {
        return _providers.GetValueOrDefault(providerKey);
    }

    /// <summary>Checks if a specific capability is supported by a provider.</summary>
    public bool Supports(string providerKey, TCapability capability)
    {
        return _providers.TryGetValue(providerKey, out var provider) && provider.Supports(capability);
    }

    /// <summary>Gets all registered provider keys.</summary>
    public IReadOnlyCollection<string> GetRegisteredProviders()
    {
        return _providers.Keys;
    }

    /// <summary>Gets capabilities for a specific provider, or empty set if not found.</summary>
    public IReadOnlySet<TCapability> GetCapabilities(string providerKey)
    {
        return _providers.TryGetValue(providerKey, out var provider)
            ? provider.GetCapabilities()
            : new HashSet<TCapability>();
    }
}
