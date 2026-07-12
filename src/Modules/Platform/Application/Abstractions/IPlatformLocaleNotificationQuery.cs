using Vitarish.Platform.Application.Contracts;
using Vitarish.Platform.Domain.Entities;

namespace Vitarish.Platform.Application.Abstractions;

public interface IPlatformLocaleNotificationQuery
{
    Task<IReadOnlyList<LocaleListItem>> ListAsync(CancellationToken cancellationToken = default);
    Task<LocaleNotificationRecord?> GetAsync(string normalizedLocale, CancellationToken cancellationToken = default);
    Task<LocaleNotificationRecord?> ResolveAsync(string? recipientLocale, CancellationToken cancellationToken = default);
}
