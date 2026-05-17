namespace Vitarish.Social.Infrastructure.Adapters;

[RegisterTransient]
internal sealed class SocialProviderAdapterRegistry(IEnumerable<ISocialProviderAdapter> adapters)
    : ISocialProviderAdapterRegistry
{
    private readonly IReadOnlyDictionary<SocialProvider, ISocialProviderAdapter> _adapters = adapters
        .GroupBy(a => a.Provider)
        .ToDictionary(g => g.Key, g => g.First());

    public IReadOnlyList<ISocialProviderAdapter> GetAll() => [.. _adapters.Values];

    public bool TryResolve(SocialProvider provider, out ISocialProviderAdapter adapter)
        => _adapters.TryGetValue(provider, out adapter!);
}
