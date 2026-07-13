using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Application.Abstractions;

public interface IPlatformLocaleNotificationCommand
{
    Task<LocaleNotificationCommandResult> CreateAsync(LocaleNotificationRecordWriteModel model, CancellationToken cancellationToken = default);
    Task<LocaleNotificationCommandResult> UpdateAsync(string normalizedLocale, LocaleNotificationRecordWriteModel model, long expectedVersion, CancellationToken cancellationToken = default);
}
