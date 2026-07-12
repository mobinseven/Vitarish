using Vitarish.Platform.Application.Contracts;

namespace Vitarish.Platform.Application.Abstractions;

public interface IPlatformLocaleNotificationCommand
{
    Task<PlatformCommandResult> CreateAsync(LocaleNotificationRecordWriteModel model, CancellationToken cancellationToken = default);
    Task<PlatformCommandResult> UpdateAsync(string normalizedLocale, LocaleNotificationRecordWriteModel model, CancellationToken cancellationToken = default);
}
