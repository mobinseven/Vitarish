namespace Vitarish.Clinic.Domain.Surgery;

public static class SurgeryObjectTypes
{
    public const string OwnerEntityType = "patient";
    public const string ObjectType = "patient.surgery";
    public const string CatalogOwnerEntityType = "clinic";
    public const string CatalogObjectType = "clinic.surgery-catalog";
    public const string SettingsObjectType = "clinic.surgery-settings";
}

/// <summary>Persisted as its name; append new members only.</summary>
public enum SurgeryStatus
{
    Planned,
    Cancelled,
    Completed
}

public sealed record SurgeryContact(string Name, string Destination, string Channel);

public sealed record SurgeryReminderPolicy(
    string Name,
    int MinutesBefore,
    string Subject,
    string TextBody);

/// <summary>A clinic staff member taking part in a surgery. <see cref="DisplayName"/> is resolved by the server.</summary>
public sealed record SurgeryTeamMember(string UserId, string DisplayName, string Role);

/// <summary>A clinic-managed surgery type whose values pre-fill new surgery plans.</summary>
public sealed record SurgeryCatalog(
    string Id,
    string Name,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> DefaultTeamRoles,
    IReadOnlyList<SurgeryReminderPolicy> ReminderPolicies,
    Guid ConcurrencyToken);

public sealed record SurgeryCatalogWriteModel(
    string Name,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> DefaultTeamRoles,
    IReadOnlyList<SurgeryReminderPolicy> ReminderPolicies);

/// <summary>The current clinic's own cancellation message sent to recipients when a surgery is cancelled.</summary>
public sealed record SurgerySettings(
    string CancellationSubject,
    string CancellationTextBody,
    Guid ConcurrencyToken);

public sealed record SurgerySettingsWriteModel(
    string CancellationSubject,
    string CancellationTextBody);

/// <summary>
/// Owns the built-in surgery-type identity that surgeries created before clinic-managed types still reference.
/// Each clinic's seeded default type answers to this id as well as its own.
/// </summary>
public static class SurgeryCatalogDefaults
{
    public const string LegacyGeneralSurgeryId = "general-surgery";
}

/// <param name="PersonsToNotify">Legacy field: derived from the distinct <paramref name="Team"/> roles when a team is present.</param>
/// <param name="DurationMinutes">Expected duration; null when unknown.</param>
/// <param name="Location">Free-text hospital or operating room.</param>
/// <param name="Team">Null in surgeries stored before team members existed; read models normalize it to empty.</param>
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
    Guid ConcurrencyToken,
    int? DurationMinutes = null,
    string? Location = null,
    IReadOnlyList<SurgeryTeamMember>? Team = null);

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
    DateTime? UpdatedAt,
    int? DurationMinutes,
    string? Location,
    IReadOnlyList<SurgeryTeamMember> Team);

/// <summary>A surgery placed on the clinic calendar, with the patient's display name.</summary>
public sealed record SurgeryCalendarEntry(SurgeryReadModel Surgery, string? PatientDisplayName);

/// <summary>A proposed time slot checked against other planned surgeries.</summary>
public sealed record SurgeryConflictProbe(
    DateTimeOffset ScheduledAt,
    int? DurationMinutes,
    string? Location,
    IReadOnlyList<string> TeamUserIds,
    Guid? ExcludeSurgeryId);

public enum SurgeryConflictReason
{
    SameLocation,
    SharedTeamMember
}

public sealed record SurgeryConflict(
    SurgeryCalendarEntry Entry,
    IReadOnlyList<SurgeryConflictReason> Reasons,
    IReadOnlyList<string> SharedTeamUserIds);

/// <summary>Previously used free-text values, most used first.</summary>
public sealed record SurgerySuggestions(IReadOnlyList<string> Locations, IReadOnlyList<string> Roles);

public static class SurgeryChannels
{
    public const string Email = "email";
    public const string Sms = "sms";

    public static bool IsSupported(string value)
        => string.Equals(value, Email, StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, Sms, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Owns surgery-plan invariants and status transitions.</summary>
public static class SurgeryPlanRules
{
    public const int MaxDurationMinutes = 24 * 60;
    public const int MaxLocationLength = 200;
    public const int MaxRoleLength = 100;
    public const int MaxTeamSize = 20;
    public const int MaxCancellationSubjectLength = 200;
    public const int MaxCancellationTextLength = 1000;

    /// <summary>Throws <see cref="ArgumentException"/> when the plan's own fields are invalid.</summary>
    public static void ValidatePlan(SurgeryWriteModel surgery)
    {
        if (surgery.PatientId == Guid.Empty) throw new ArgumentException("A patient is required.");
        if (string.IsNullOrWhiteSpace(surgery.CatalogId)) throw new ArgumentException("A surgery type is required.");
        if (string.IsNullOrWhiteSpace(surgery.Name)) throw new ArgumentException("A surgery name is required.");
        if (string.IsNullOrWhiteSpace(surgery.IdempotencyKey)) throw new ArgumentException("An idempotency key is required.");
        if (surgery.IdempotencyKey is { Length: > 128 }) throw new ArgumentException("The idempotency key cannot exceed 128 characters.");
        if (surgery.ScheduledAt == default) throw new ArgumentException("A surgery date and time are required.");
        if (surgery.DurationMinutes is < 1 or > MaxDurationMinutes) throw new ArgumentException($"The duration must be between 1 and {MaxDurationMinutes} minutes.");
        if (surgery.Location is { Length: > MaxLocationLength }) throw new ArgumentException($"The location cannot exceed {MaxLocationLength} characters.");
        if (surgery.ReminderPolicies.Any(policy => policy.MinutesBefore < 0)) throw new ArgumentException("Reminder offsets cannot be negative.");
        foreach (SurgeryContact contact in surgery.Contacts)
        {
            if (string.IsNullOrWhiteSpace(contact.Name) || string.IsNullOrWhiteSpace(contact.Destination) || !SurgeryChannels.IsSupported(contact.Channel))
                throw new ArgumentException("Each surgery contact needs a name, destination, and email or sms channel.");
        }

        IReadOnlyList<SurgeryTeamMember> team = surgery.Team ?? [];
        if (team.Count > MaxTeamSize) throw new ArgumentException($"A surgery team cannot have more than {MaxTeamSize} members.");
        foreach (SurgeryTeamMember member in team)
        {
            if (string.IsNullOrWhiteSpace(member.UserId)) throw new ArgumentException("Each team member needs a user.");
            if (string.IsNullOrWhiteSpace(member.Role)) throw new ArgumentException("Each team member needs a role.");
            if (member.Role.Trim().Length > MaxRoleLength) throw new ArgumentException($"A team role cannot exceed {MaxRoleLength} characters.");
        }

        if (team.Select(member => member.UserId.Trim()).Distinct(StringComparer.Ordinal).Count() != team.Count)
            throw new ArgumentException("A team member can appear only once.");
    }

    public static void ValidateCatalog(SurgeryCatalogWriteModel catalog)
    {
        if (string.IsNullOrWhiteSpace(catalog.Name)) throw new ArgumentException("A surgery type name is required.");
        if (catalog.ReminderPolicies.Any(policy => policy.MinutesBefore < 0)) throw new ArgumentException("Reminder offsets cannot be negative.");
        if (catalog.ReminderPolicies.Any(policy => string.IsNullOrWhiteSpace(policy.Subject) || string.IsNullOrWhiteSpace(policy.TextBody)))
            throw new ArgumentException("Each reminder needs a subject and a message.");
        if (catalog.DefaultTeamRoles.Any(role => string.IsNullOrWhiteSpace(role) || role.Trim().Length > MaxRoleLength))
            throw new ArgumentException($"Each default team role needs a value of at most {MaxRoleLength} characters.");
    }

    public static void ValidateSettings(SurgerySettingsWriteModel settings)
    {
        if (string.IsNullOrWhiteSpace(settings.CancellationSubject)) throw new ArgumentException("A cancellation subject is required.");
        if (settings.CancellationSubject.Trim().Length > MaxCancellationSubjectLength) throw new ArgumentException($"The cancellation subject cannot exceed {MaxCancellationSubjectLength} characters.");
        if (string.IsNullOrWhiteSpace(settings.CancellationTextBody)) throw new ArgumentException("A cancellation message is required.");
        if (settings.CancellationTextBody.Trim().Length > MaxCancellationTextLength) throw new ArgumentException($"The cancellation message cannot exceed {MaxCancellationTextLength} characters.");
    }

    /// <summary>Trims free text and maps blank values to null.</summary>
    public static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>The distinct, non-blank roles of a team, in first-seen order.</summary>
    public static IReadOnlyList<string> TeamRoles(IReadOnlyList<SurgeryTeamMember> team)
        => team.Select(member => member.Role.Trim()).Where(role => role.Length > 0).Distinct(StringComparer.Ordinal).ToArray();

    /// <summary>Planned and completed surgeries can be edited; editing a completed one corrects a mistaken completion.</summary>
    public static void EnsureEditable(SurgeryReadModel current)
    {
        if (current.Status is not (SurgeryStatus.Planned or SurgeryStatus.Completed)) throw new InvalidOperationException("Only a planned or completed surgery can be edited.");
    }

    /// <summary>An edit keeps a surgery planned or completed; it can be completed only once its scheduled start has passed.</summary>
    public static void EnsureEditedStatus(SurgeryStatus status, DateTimeOffset scheduledAt, DateTimeOffset now)
    {
        if (status is not (SurgeryStatus.Planned or SurgeryStatus.Completed)) throw new ArgumentException("An edited surgery must be planned or completed.", nameof(status));
        if (status == SurgeryStatus.Completed && scheduledAt > now) throw new ArgumentException("A surgery cannot be completed before its scheduled start.", nameof(scheduledAt));
    }

    /// <summary>A planned surgery can be completed once its scheduled start has passed.</summary>
    public static void EnsureCompletable(SurgeryReadModel current, DateTimeOffset now)
    {
        if (current.Status != SurgeryStatus.Planned) throw new InvalidOperationException("Only a planned surgery can be completed.");
        if (current.ScheduledAt > now) throw new InvalidOperationException("A surgery cannot be completed before its scheduled start.");
    }

    /// <summary>Only planned surgeries can be cancelled, because cancelling notifies the patient and the team; a mistaken completion is corrected by editing the surgery's status.</summary>
    public static bool IsCancellable(SurgeryStatus status)
        => status == SurgeryStatus.Planned;

    /// <summary>
    /// Whether two surgeries occupy overlapping time. A surgery without a duration occupies only its start instant,
    /// so two surgeries without durations overlap only when they start at the same time.
    /// </summary>
    public static bool Overlaps(DateTimeOffset firstStart, int? firstDurationMinutes, DateTimeOffset secondStart, int? secondDurationMinutes)
    {
        DateTimeOffset firstEnd = firstStart.AddMinutes(firstDurationMinutes ?? 0);
        DateTimeOffset secondEnd = secondStart.AddMinutes(secondDurationMinutes ?? 0);
        if (firstDurationMinutes is null && secondDurationMinutes is null) return firstStart == secondStart;
        if (firstDurationMinutes is null) return firstStart >= secondStart && firstStart < secondEnd;
        if (secondDurationMinutes is null) return secondStart >= firstStart && secondStart < firstEnd;
        return firstStart < secondEnd && secondStart < firstEnd;
    }

    /// <summary>Locations match ignoring case and surrounding or repeated whitespace.</summary>
    public static bool SameLocation(string? first, string? second)
    {
        string? a = CollapseWhitespace(first);
        string? b = CollapseWhitespace(second);
        return a is not null && b is not null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static string? CollapseWhitespace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
