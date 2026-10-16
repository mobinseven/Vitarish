namespace Vitarish.Clinic.Contracts;

public sealed record SurgeryContactDto(string Name, string Destination, string Channel);

public sealed record SurgeryReminderPolicyDto(string Name, int MinutesBefore, string Subject, string TextBody);

/// <summary>A surgery team member. The server ignores a submitted <see cref="DisplayName"/> and resolves it from the clinic's members.</summary>
public sealed record SurgeryTeamMemberDto(string UserId, string DisplayName, string Role);

/// <summary>A clinic-managed surgery type ("catalog" on the wire for compatibility).</summary>
public sealed record SurgeryCatalogDto(
    string Id,
    string Name,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> DefaultTeamRoles,
    IReadOnlyList<SurgeryReminderPolicyDto> ReminderPolicies,
    Guid ConcurrencyToken);

public sealed record SurgeryCatalogWriteRequest(
    string Name,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> DefaultTeamRoles,
    IReadOnlyList<SurgeryReminderPolicyDto> ReminderPolicies);

public sealed record SurgeryCatalogUpdateRequest(
    string Name,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> DefaultTeamRoles,
    IReadOnlyList<SurgeryReminderPolicyDto> ReminderPolicies,
    Guid ConcurrencyToken);

public sealed record SurgeryCatalogDeleteRequest(Guid ConcurrencyToken);

/// <summary>The current clinic's own cancellation message sent to recipients when a surgery is cancelled.</summary>
public sealed record SurgerySettingsDto(
    string CancellationSubject,
    string CancellationTextBody,
    Guid ConcurrencyToken);

public sealed record SurgerySettingsWriteRequest(
    string CancellationSubject,
    string CancellationTextBody);

public sealed record SurgerySettingsUpdateRequest(
    string CancellationSubject,
    string CancellationTextBody,
    Guid ConcurrencyToken);

/// <param name="PersonsToNotify">Legacy: ignored when <paramref name="Team"/> has members, whose roles replace it.</param>
public sealed record SurgeryCreateRequest(
    string CatalogId,
    string Name,
    string Status,
    DateTimeOffset ScheduledAt,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> PersonsToNotify,
    IReadOnlyList<SurgeryContactDto> Contacts,
    IReadOnlyList<SurgeryReminderPolicyDto> ReminderPolicies,
    string IdempotencyKey,
    int? DurationMinutes = null,
    string? Location = null,
    IReadOnlyList<SurgeryTeamMemberDto>? Team = null);

/// <param name="PersonsToNotify">Legacy: ignored when <paramref name="Team"/> has members, whose roles replace it.</param>
public sealed record SurgeryWriteRequest(
    string CatalogId,
    string Name,
    string Status,
    DateTimeOffset ScheduledAt,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> PersonsToNotify,
    IReadOnlyList<SurgeryContactDto> Contacts,
    IReadOnlyList<SurgeryReminderPolicyDto> ReminderPolicies,
    Guid ConcurrencyToken,
    int? DurationMinutes = null,
    string? Location = null,
    IReadOnlyList<SurgeryTeamMemberDto>? Team = null);

public sealed record SurgeryCancelRequest(Guid ConcurrencyToken);

public sealed record SurgeryCompleteRequest(Guid ConcurrencyToken);

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
    DateTime? UpdatedAt,
    int? DurationMinutes,
    string? Location,
    IReadOnlyList<SurgeryTeamMemberDto> Team);

/// <summary>A surgery with its patient's display name, as shown on calendars and the detail page.</summary>
public sealed record SurgeryCalendarEntryResponse(SurgeryResponse Surgery, string? PatientDisplayName);

/// <param name="from">Inclusive start instant.</param>
/// <param name="to">Exclusive end instant; at most <see cref="SurgeryCalendarLimits.MaxRangeDays"/> days after <paramref name="from"/>.</param>
public sealed record SurgeryCalendarRequest(DateTimeOffset from, DateTimeOffset to);

public static class SurgeryCalendarLimits
{
    public const int MaxRangeDays = 62;
}

public sealed record SurgeryConflictRequest(
    DateTimeOffset scheduledAt,
    int? durationMinutes = null,
    string? location = null,
    string[]? teamUserIds = null,
    Guid? excludeSurgeryId = null);

/// <param name="Reasons">Values of <c>SurgeryConflictReason</c>: <c>SameLocation</c>, <c>SharedTeamMember</c>.</param>
public sealed record SurgeryConflictResponse(
    SurgeryCalendarEntryResponse Entry,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> SharedTeamUserIds);

public sealed record SurgerySuggestionsResponse(IReadOnlyList<string> Locations, IReadOnlyList<string> Roles);
