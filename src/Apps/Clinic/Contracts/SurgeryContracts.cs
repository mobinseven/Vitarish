namespace Vitarish.Clinic.Contracts;

public sealed record SurgeryContactDto(string Name, string Destination, string Channel);

public sealed record SurgeryReminderPolicyDto(string Name, int MinutesBefore, string Subject, string TextBody);

public sealed record SurgeryCatalogDto(
    string Id,
    string Name,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> PersonsToNotify,
    IReadOnlyList<SurgeryReminderPolicyDto> ReminderPolicies);

public sealed record SurgeryCreateRequest(
    string CatalogId,
    string Name,
    string Status,
    DateTimeOffset ScheduledAt,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> PersonsToNotify,
    IReadOnlyList<SurgeryContactDto> Contacts,
    IReadOnlyList<SurgeryReminderPolicyDto> ReminderPolicies,
    string IdempotencyKey);

public sealed record SurgeryWriteRequest(
    string CatalogId,
    string Name,
    string Status,
    DateTimeOffset ScheduledAt,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> PersonsToNotify,
    IReadOnlyList<SurgeryContactDto> Contacts,
    IReadOnlyList<SurgeryReminderPolicyDto> ReminderPolicies,
    Guid ConcurrencyToken);

public sealed record SurgeryCancelRequest(Guid ConcurrencyToken);

public sealed record SurgeryResponse(
    Guid Id,
    Guid PatientId,
    string CatalogId,
    string Name,
    string? IdempotencyKey,
    string Status,
    DateTimeOffset ScheduledAt,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> PersonsToNotify,
    IReadOnlyList<SurgeryContactDto> Contacts,
    IReadOnlyList<SurgeryReminderPolicyDto> ReminderPolicies,
    Guid ConcurrencyToken,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
