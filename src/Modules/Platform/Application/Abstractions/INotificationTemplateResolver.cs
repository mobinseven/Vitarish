using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Application.Abstractions;

public interface INotificationTemplateResolver
{
    Task<ResolvedNotificationTemplate?> ResolveAsync(string? recipientLocale, NotificationPurpose purpose, CancellationToken cancellationToken = default);
}
