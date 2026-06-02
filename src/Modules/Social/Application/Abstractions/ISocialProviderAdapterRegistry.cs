using Vitarish.Social.Domain.Enums;

namespace Vitarish.Social.Application.Abstractions;

public interface ISocialProviderAdapterRegistry
{
    bool TryResolve(SocialProvider provider, out ISocialProviderAdapter adapter);
    IReadOnlyList<ISocialProviderAdapter> GetAll();
}
