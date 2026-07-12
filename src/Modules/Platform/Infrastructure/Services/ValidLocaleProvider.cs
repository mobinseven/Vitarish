using Vitarish.Modules.Base.Core.Abstractions;
using Vitarish.Platform.Application.Abstractions;

namespace Vitarish.Platform.Infrastructure.Services;

public sealed class ValidLocaleProvider(IPlatformLocaleNotificationQuery query) : IValidLocaleProvider
{
    public async Task<IReadOnlyList<string>> GetValidLocalesAsync(CancellationToken cancellationToken = default)
    {
        var items = await query.ListAsync(cancellationToken);
        return items.Select(i => i.NormalizedLocale).ToList();
    }

    public async Task<bool> IsValidLocaleAsync(string locale, CancellationToken cancellationToken = default)
    {
        var items = await query.ListAsync(cancellationToken);
        return items.Any(i => i.NormalizedLocale == locale);
    }
}
