using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Clinic.Domain.Surgery;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Surgery;

/// <summary>
/// What the clinician reviews before confirming a surgery-workflow action; the Web surgery review card reads it.
/// An edit carries the state it was prepared against as its previous value. It never carries a patient id.
/// </summary>
public sealed record SurgeryAssistantReview(
    string ActionType,
    SurgeryReviewPatient? Patient = null,
    SurgeryReviewPlan? Surgery = null,
    SurgeryReviewPlan? PreviousSurgery = null,
    SurgeryReviewType? SurgeryType = null,
    SurgeryReviewType? PreviousSurgeryType = null,
    SurgeryReviewMessage? CancellationMessage = null,
    SurgeryReviewMessage? PreviousCancellationMessage = null,
    bool NotifiesPatientAndTeam = false);

public sealed record SurgeryReviewPatient(string DisplayName, string? MaskedIdentification = null);

public sealed record SurgeryReviewTeamMember(string DisplayName, string Role);

public sealed record SurgeryReviewReminder(string Name, int MinutesBefore, string Subject, string TextBody);

public sealed record SurgeryReviewPlan(
    string Name,
    string? TypeName,
    SurgeryStatus Status,
    DateTimeOffset ScheduledAt,
    int? DurationMinutes,
    string? Location,
    IReadOnlyList<SurgeryReviewTeamMember> Team,
    IReadOnlyList<string> Tools,
    IReadOnlyList<SurgeryReviewReminder> Reminders);

public sealed record SurgeryReviewType(
    string Name,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> DefaultTeamRoles,
    IReadOnlyList<SurgeryReviewReminder> Reminders);

public sealed record SurgeryReviewMessage(string Subject, string TextBody);

/// <summary>A preparation tool's result: the review and the server-side proposal it stands for.</summary>
internal sealed record SurgeryAssistantPreparation(
    string Status,
    Guid ProposalId,
    string ActionType,
    DateTimeOffset ExpiresAt,
    string Summary,
    SurgeryAssistantReview Review)
{
    public const string Prepared = "prepared";
}

internal static class SurgeryReviews
{
    public static SurgeryReviewPlan Plan(SurgeryWriteModel surgery, string? typeName, IReadOnlyList<SurgeryTeamMember> team) => new(
        surgery.Name,
        typeName,
        surgery.Status,
        surgery.ScheduledAt,
        surgery.DurationMinutes,
        surgery.Location,
        team.Select(member => new SurgeryReviewTeamMember(member.DisplayName, member.Role)).ToArray(),
        surgery.Tools,
        Reminders(surgery.ReminderPolicies));

    public static SurgeryReviewPlan Plan(SurgeryReadModel surgery, string? typeName) => new(
        surgery.Name,
        typeName,
        surgery.Status,
        surgery.ScheduledAt,
        surgery.DurationMinutes,
        surgery.Location,
        surgery.Team.Select(member => new SurgeryReviewTeamMember(member.DisplayName, member.Role)).ToArray(),
        surgery.Tools,
        Reminders(surgery.ReminderPolicies));

    public static SurgeryReviewType Type(SurgeryCatalogWriteModel type) =>
        new(type.Name, type.Tools, type.DefaultTeamRoles, Reminders(type.ReminderPolicies));

    public static SurgeryReviewType Type(SurgeryCatalog type) =>
        new(type.Name, type.Tools, type.DefaultTeamRoles, Reminders(type.ReminderPolicies));

    private static SurgeryReviewReminder[] Reminders(IReadOnlyList<SurgeryReminderPolicy> policies) =>
        policies.Select(policy => new SurgeryReviewReminder(policy.Name, policy.MinutesBefore, policy.Subject, policy.TextBody)).ToArray();
}

/// <summary>Practice-scope views of surgery data: they name the patient but never carry a patient id or contact details.</summary>
internal sealed record SurgeryOperationsView(
    Guid Id,
    string? PatientDisplayName,
    string CatalogId,
    string Name,
    SurgeryStatus Status,
    DateTimeOffset ScheduledAt,
    int? DurationMinutes,
    string? Location,
    IReadOnlyList<SurgeryTeamMember> Team,
    IReadOnlyList<string> Tools,
    IReadOnlyList<SurgeryReminderPolicy> ReminderPolicies)
{
    public static SurgeryOperationsView From(SurgeryCalendarEntry entry) => new(
        entry.Surgery.Id,
        entry.PatientDisplayName,
        entry.Surgery.CatalogId,
        entry.Surgery.Name,
        entry.Surgery.Status,
        entry.Surgery.ScheduledAt,
        entry.Surgery.DurationMinutes,
        entry.Surgery.Location,
        entry.Surgery.Team,
        entry.Surgery.Tools,
        entry.Surgery.ReminderPolicies);
}

internal sealed record SurgeryStaffView(string UserId, string DisplayName);

/// <summary>A conflicting surgery; the patient is named only in the practice scope, never to another patient's assistant.</summary>
internal sealed record SurgeryConflictView(
    Guid SurgeryId,
    string Name,
    string? PatientDisplayName,
    DateTimeOffset ScheduledAt,
    int? DurationMinutes,
    string? Location,
    IReadOnlyList<SurgeryConflictReason> Reasons,
    IReadOnlyList<string> SharedTeamUserIds);

internal sealed record SurgeryAssistantListResult(IReadOnlyList<SurgeryReadModel> Surgeries);
internal sealed record SurgeryOperationsListResult(IReadOnlyList<SurgeryOperationsView> Surgeries);
internal sealed record SurgeryAssistantTypesResult(
    [property: JsonPropertyName("types")] IReadOnlyList<SurgeryCatalog> Types);
internal sealed record SurgeryStaffResult(IReadOnlyList<SurgeryStaffView> Staff);
internal sealed record SurgeryConflictsResult(IReadOnlyList<SurgeryConflictView> Conflicts);
internal sealed record SurgeryCancellationMessageResult(SurgeryReviewMessage? CancellationMessage);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(SurgeryAssistantPreparation))]
[JsonSerializable(typeof(SurgeryAssistantReview))]
[JsonSerializable(typeof(SurgeryAssistantListResult))]
[JsonSerializable(typeof(SurgeryOperationsListResult))]
[JsonSerializable(typeof(SurgeryOperationsView))]
[JsonSerializable(typeof(SurgeryAssistantTypesResult))]
[JsonSerializable(typeof(SurgeryStaffResult))]
[JsonSerializable(typeof(SurgeryConflictsResult))]
[JsonSerializable(typeof(SurgerySuggestions))]
[JsonSerializable(typeof(SurgeryCancellationMessageResult))]
[JsonSerializable(typeof(SurgeryReadModel))]
[JsonSerializable(typeof(SurgeryCatalog))]
[JsonSerializable(typeof(SurgerySettings))]
internal sealed partial class SurgeryAssistantJsonSerializerContext : JsonSerializerContext
{
    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>)Default.GetTypeInfo(typeof(T))!);
}
