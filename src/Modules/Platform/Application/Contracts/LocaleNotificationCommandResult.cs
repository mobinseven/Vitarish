namespace Vitarish.Platform.Application.Contracts;

public enum LocaleNotificationCommandStatus
{
    Success,
    Validation,
    NotFound,
    DuplicateLocale,
    StaleVersion
}

public sealed record LocaleNotificationCommandResult(
    LocaleNotificationCommandStatus Status,
    LocaleNotificationValidationFailure? ValidationFailure = null)
{
    public bool Succeeded => Status == LocaleNotificationCommandStatus.Success;

    public static LocaleNotificationCommandResult Success() => new(LocaleNotificationCommandStatus.Success);

    public static LocaleNotificationCommandResult Validation(LocaleNotificationValidationFailure failure)
        => new(LocaleNotificationCommandStatus.Validation, failure);

    public static LocaleNotificationCommandResult NotFound() => new(LocaleNotificationCommandStatus.NotFound);

    public static LocaleNotificationCommandResult DuplicateLocale() => new(LocaleNotificationCommandStatus.DuplicateLocale);

    public static LocaleNotificationCommandResult StaleVersion() => new(LocaleNotificationCommandStatus.StaleVersion);
}
