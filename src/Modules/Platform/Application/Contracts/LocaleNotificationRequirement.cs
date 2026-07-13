namespace Vitarish.Platform.Application.Contracts;

public enum LocaleNotificationPurpose
{
    SeaReminder,
    AppointmentReminder
}

public sealed record LocaleNotificationRequirement(
    LocaleNotificationPurpose Purpose,
    IReadOnlyList<string> Fields,
    IReadOnlyList<string> RequiredPlaceholders,
    IReadOnlyList<string> AllowedPlaceholders);
