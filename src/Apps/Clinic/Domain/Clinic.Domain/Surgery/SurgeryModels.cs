namespace Vitarish.Clinic.Domain.Surgery;

public static class SurgeryObjectTypes
{
    public const string OwnerEntityType = "patient";
    public const string ObjectType = "patient.surgery";
}

public enum SurgeryStatus
{
    Planned,
    Cancelled
}

public sealed record SurgeryContact(string Name, string Destination, string Channel);

public sealed record SurgeryReminderPolicy(
    string Name,
    int MinutesBefore,
    string Subject,
    string TextBody);

public sealed record SurgeryCatalog(
    string Id,
    string Name,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> PersonsToNotify,
    IReadOnlyList<SurgeryReminderPolicy> ReminderPolicies);

public static class SurgeryCatalogs
{
    public static IReadOnlyList<SurgeryCatalog> All { get; } =
    [
        new SurgeryCatalog(
            "general-surgery",
            "General surgery",
            ["Operating room", "Surgical instruments"],
            ["Surgeon", "Assistant"],
            [new SurgeryReminderPolicy("One day before", 1440, "Surgery reminder", "The scheduled surgery is tomorrow.")])
    ];

    public static SurgeryCatalog? Find(string id) => All.FirstOrDefault(item => item.Id == id);
}

public sealed record SurgeryWriteModel(
    Guid Id,
    Guid PatientId,
    string CatalogId,
    string Name,
    string? IdempotencyKey,
    SurgeryStatus Status,
    DateTimeOffset ScheduledAt,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> PersonsToNotify,
    IReadOnlyList<SurgeryContact> Contacts,
    IReadOnlyList<SurgeryReminderPolicy> ReminderPolicies,
    Guid ConcurrencyToken);

public sealed record SurgeryReadModel(
    Guid Id,
    Guid PatientId,
    string CatalogId,
    string Name,
    string? IdempotencyKey,
    SurgeryStatus Status,
    DateTimeOffset ScheduledAt,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> PersonsToNotify,
    IReadOnlyList<SurgeryContact> Contacts,
    IReadOnlyList<SurgeryReminderPolicy> ReminderPolicies,
    Guid ConcurrencyToken,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public static class SurgeryChannels
{
    public static bool IsSupported(string value)
        => string.Equals(value, "email", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "sms", StringComparison.OrdinalIgnoreCase);
}
