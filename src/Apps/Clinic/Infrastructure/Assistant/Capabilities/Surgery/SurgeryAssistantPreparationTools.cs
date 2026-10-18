using System.Text.Json;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Assistant.Surgeries;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Surgery;

/// <summary>What a preparation writes on confirmation, and the review the clinician confirms it from.</summary>
public sealed record SurgeryPreparationDraft(
    SurgeryAssistantReview Review,
    string Summary,
    SurgeryWriteModel? Surgery = null,
    Guid? SurgeryId = null,
    Guid? SurgeryTypeId = null,
    Guid ExpectedConcurrencyToken = default,
    SurgeryCatalogWriteModel? SurgeryType = null,
    SurgerySettingsWriteModel? CancellationMessage = null);

/// <summary>
/// Prepares one surgery-workflow action for explicit confirmation: it checks the request against the current state,
/// keeps what the confirmation writes on the server, and returns only the review. It never changes anything itself.
/// </summary>
public abstract class SurgeryPreparationTool(
    string name,
    string actionType,
    string capability,
    string description,
    string schema,
    ISurgeryAssistantProposalStore proposals,
    TimeProvider timeProvider) : IAssistantPreparationTool
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    public AgentToolDefinition Definition { get; } = new(name, description + " This prepares it for the clinician's explicit confirmation and does not change anything.", schema);
    protected string ActionType { get; } = actionType;

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        SurgeryAssistantScope scope = SurgeryAssistantScope.From(context);
        DateTimeOffset now = timeProvider.GetUtcNow();
        Guid proposalId = Guid.CreateVersion7();
        SurgeryPreparationDraft draft = await PrepareAsync(document.RootElement, scope, proposalId, now, cancellationToken);
        DateTimeOffset expiresAt = now.Add(Lifetime);
        await proposals.SaveAsync(new SurgeryAssistantProposal(
            proposalId,
            ActionType,
            scope.PracticeId,
            scope.UserId,
            scope.SubjectKind,
            scope.IsPatient ? scope.PatientId : Guid.Empty,
            expiresAt,
            draft.Surgery,
            draft.SurgeryId,
            draft.SurgeryTypeId,
            draft.ExpectedConcurrencyToken,
            draft.SurgeryType,
            draft.CancellationMessage), cancellationToken);
        return new(call.Id, SurgeryAssistantJsonSerializerContext.Serialize(new SurgeryAssistantPreparation(
            SurgeryAssistantPreparation.Prepared, proposalId, ActionType, expiresAt, draft.Summary, draft.Review)));
    }

    public AssistantPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        SurgeryAssistantPreparation? preparation = JsonSerializer.Deserialize(
            result.Output, SurgeryAssistantJsonSerializerContext.Default.SurgeryAssistantPreparation);
        return preparation is { Status: SurgeryAssistantPreparation.Prepared } && preparation.ActionType == ActionType
            ? new(
                capability,
                ActionType,
                preparation.ProposalId,
                JsonSerializer.Serialize(preparation.Review, SurgeryAssistantJsonSerializerContext.Default.SurgeryAssistantReview),
                preparation.ExpiresAt,
                preparation.Summary)
            : null;
    }

    private protected abstract Task<SurgeryPreparationDraft> PrepareAsync(
        JsonElement arguments, SurgeryAssistantScope scope, Guid proposalId, DateTimeOffset now, CancellationToken cancellationToken);

    protected SurgeryAssistantReview Review(
        SurgeryReviewPatient? patient = null,
        SurgeryReviewPlan? surgery = null,
        SurgeryReviewPlan? previousSurgery = null,
        SurgeryReviewType? surgeryType = null,
        SurgeryReviewType? previousSurgeryType = null,
        SurgeryReviewMessage? cancellationMessage = null,
        SurgeryReviewMessage? previousCancellationMessage = null,
        bool notifiesPatientAndTeam = false) =>
        new(ActionType, patient, surgery, previousSurgery, surgeryType, previousSurgeryType,
            cancellationMessage, previousCancellationMessage, notifiesPatientAndTeam);

    /// <summary>A surgery the current scope may change: any surgery of the practice, or only the route-bound patient's.</summary>
    private protected static async Task<SurgeryCalendarEntry> FindSurgeryAsync(
        ISurgeryToolchain surgeries, JsonElement arguments, SurgeryAssistantScope scope, CancellationToken cancellationToken)
    {
        Guid surgeryId = SurgeryAssistantArguments.RequiredGuid(arguments, "surgeryId");
        SurgeryCalendarEntry? entry = await surgeries.GetAsync(surgeryId, cancellationToken);
        return entry is not null && (!scope.IsPatient || entry.Surgery.PatientId == scope.PatientId)
            ? entry
            : throw new ArgumentException("The surgery does not exist.", "surgeryId");
    }

    /// <summary>The patient the review names; the patient's own assistant already shows who it is about.</summary>
    private protected static SurgeryReviewPatient? ReviewPatient(SurgeryAssistantScope scope, string? displayName) =>
        scope.IsPatient || string.IsNullOrWhiteSpace(displayName) ? null : new(displayName);

    protected static async Task<SurgeryCatalog> FindTypeAsync(ISurgeryCatalogStore catalogs, string catalogId, CancellationToken cancellationToken) =>
        await catalogs.FindAsync(catalogId, cancellationToken) ?? throw new ArgumentException("The surgery type does not exist; list the clinic's surgery types first.", "catalogId");

    /// <summary>Resolves team members against the clinic staff so the review shows names and a stranger is refused before confirmation.</summary>
    protected static async Task<IReadOnlyList<SurgeryTeamMember>> ResolveTeamAsync(
        ISurgeryTeamDirectory directory, IReadOnlyList<SurgeryTeamMember> team, CancellationToken cancellationToken)
    {
        if (team.Count == 0) return [];
        Dictionary<string, SurgeryTeamContact> members = (await directory.FindMembersAsync(team.Select(member => member.UserId).ToArray(), cancellationToken))
            .ToDictionary(member => member.UserId, StringComparer.Ordinal);
        return team.Select(member => members.TryGetValue(member.UserId, out SurgeryTeamContact? contact)
                ? member with { DisplayName = contact.DisplayName }
                : throw new ArgumentException("Each team member must be clinic staff; list the clinic staff first.", "team"))
            .ToArray();
    }
}

/// <summary>Plans a new surgery for the route-bound patient, or in the practice scope for a patient found by search.</summary>
public sealed class PrepareSurgeryCreationTool(
    string name,
    string actionType,
    string capability,
    ISurgeryCatalogStore catalogs,
    ISurgeryTeamDirectory directory,
    IAssistantPatientSearch patients,
    ISurgeryAssistantProposalStore proposals,
    TimeProvider timeProvider) : SurgeryPreparationTool(
        name,
        actionType,
        capability,
        "Prepare a new surgery plan. Tools and reminders default to the surgery type's when omitted.",
        actionType == SurgeryAssistantActions.Create ? SurgeryAssistantSchemas.PatientPlan : SurgeryAssistantSchemas.PracticePlan,
        proposals,
        timeProvider)
{
    private protected override async Task<SurgeryPreparationDraft> PrepareAsync(
        JsonElement arguments, SurgeryAssistantScope scope, Guid proposalId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        (Guid patientId, SurgeryReviewPatient? patient) = await PatientAsync(arguments, scope, cancellationToken);
        SurgeryCatalog type = await FindTypeAsync(catalogs, SurgeryAssistantArguments.RequiredText(arguments, "catalogId"), cancellationToken);
        IReadOnlyList<SurgeryTeamMember> team = await ResolveTeamAsync(directory, SurgeryAssistantArguments.Team(arguments) ?? [], cancellationToken);
        SurgeryWriteModel surgery = new(
            Guid.Empty,
            patientId,
            type.Id,
            SurgeryAssistantArguments.RequiredText(arguments, "name"),
            // A retried confirmation answers with the surgery its first attempt created.
            proposalId.ToString("N"),
            SurgeryStatus.Planned,
            SurgeryAssistantArguments.Instant(arguments, "scheduledAt") ?? throw new ArgumentException("scheduledAt is required.", "scheduledAt"),
            SurgeryAssistantArguments.Strings(arguments, "tools") ?? type.Tools,
            SurgeryAssistantArguments.Strings(arguments, "personsToNotify") ?? [],
            SurgeryAssistantArguments.Contacts(arguments) ?? [],
            SurgeryAssistantArguments.Reminders(arguments, "reminderPolicies") ?? type.ReminderPolicies,
            Guid.Empty,
            SurgeryAssistantArguments.Integer(arguments, "durationMinutes"),
            SurgeryPlanRules.NormalizeText(SurgeryAssistantArguments.Text(arguments, "location")),
            team);
        SurgeryPlanRules.ValidatePlan(surgery);
        return new(
            Review(patient, SurgeryReviews.Plan(surgery, type.Name, team)),
            $"Plan surgery '{surgery.Name}' at {surgery.ScheduledAt:O}{(surgery.Location is { } location ? $" in {location}" : string.Empty)}.",
            Surgery: surgery);
    }

    private async Task<(Guid PatientId, SurgeryReviewPatient? Patient)> PatientAsync(
        JsonElement arguments, SurgeryAssistantScope scope, CancellationToken cancellationToken)
    {
        if (scope.IsPatient) return (scope.PatientId, null);
        AssistantCandidateReference candidate = await patients.ResolveAsync(
            scope.PracticeId, scope.UserId, scope.AgentRunId, SurgeryAssistantArguments.RequiredText(arguments, "candidateToken"), cancellationToken)
            ?? throw new ArgumentException("The patient token is unknown or expired; search the patient again.", "candidateToken");
        return (candidate.PatientId, new(candidate.DisplayName, candidate.MaskedIdentification));
    }
}

/// <summary>Edits a planned or completed surgery; omitted fields keep their current value.</summary>
public sealed class PrepareSurgeryUpdateTool(
    string name,
    string actionType,
    string capability,
    ISurgeryToolchain surgeries,
    ISurgeryCatalogStore catalogs,
    ISurgeryTeamDirectory directory,
    ISurgeryAssistantProposalStore proposals,
    TimeProvider timeProvider) : SurgeryPreparationTool(
        name,
        actionType,
        capability,
        "Prepare an edit of a planned or completed surgery; only the given fields change. A team, tools, or reminders list replaces the current one. Set status to planned to correct a mistaken completion.",
        SurgeryAssistantSchemas.Edit,
        proposals,
        timeProvider)
{
    private protected override async Task<SurgeryPreparationDraft> PrepareAsync(
        JsonElement arguments, SurgeryAssistantScope scope, Guid proposalId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        SurgeryCalendarEntry entry = await FindSurgeryAsync(surgeries, arguments, scope, cancellationToken);
        SurgeryReadModel current = entry.Surgery;
        SurgeryPlanRules.EnsureEditable(current);
        string catalogId = SurgeryPlanRules.NormalizeText(SurgeryAssistantArguments.Text(arguments, "catalogId")) ?? current.CatalogId;
        SurgeryCatalog? currentType = await catalogs.FindAsync(current.CatalogId, cancellationToken);
        SurgeryCatalog? type = catalogId == current.CatalogId ? currentType : await FindTypeAsync(catalogs, catalogId, cancellationToken);
        IReadOnlyList<SurgeryTeamMember> team = SurgeryAssistantArguments.Team(arguments) is { } requestedTeam
            ? await ResolveTeamAsync(directory, requestedTeam, cancellationToken)
            : current.Team;
        SurgeryWriteModel surgery = new(
            current.Id,
            current.PatientId,
            catalogId,
            SurgeryPlanRules.NormalizeText(SurgeryAssistantArguments.Text(arguments, "name")) ?? current.Name,
            current.IdempotencyKey ?? proposalId.ToString("N"),
            SurgeryAssistantArguments.EditedStatus(arguments) ?? current.Status,
            SurgeryAssistantArguments.Instant(arguments, "scheduledAt") ?? current.ScheduledAt,
            SurgeryAssistantArguments.Strings(arguments, "tools") ?? current.Tools,
            current.PersonsToNotify,
            current.Contacts,
            SurgeryAssistantArguments.Reminders(arguments, "reminderPolicies") ?? current.ReminderPolicies,
            current.ConcurrencyToken,
            SurgeryAssistantArguments.Has(arguments, "durationMinutes") ? SurgeryAssistantArguments.Integer(arguments, "durationMinutes") : current.DurationMinutes,
            SurgeryAssistantArguments.Has(arguments, "location") ? SurgeryPlanRules.NormalizeText(SurgeryAssistantArguments.Text(arguments, "location")) : current.Location,
            team);
        SurgeryPlanRules.EnsureEditedStatus(surgery.Status, surgery.ScheduledAt, now);
        SurgeryPlanRules.ValidatePlan(surgery);
        SurgeryReviewPlan before = SurgeryReviews.Plan(current, currentType?.Name);
        SurgeryReviewPlan after = SurgeryReviews.Plan(surgery, type?.Name, team);
        if (JsonSerializer.Serialize(before, SurgeryAssistantJsonSerializerContext.Default.SurgeryReviewPlan) ==
            JsonSerializer.Serialize(after, SurgeryAssistantJsonSerializerContext.Default.SurgeryReviewPlan) &&
            catalogId == current.CatalogId)
            throw new ArgumentException("The edit changes nothing.");
        return new(
            Review(ReviewPatient(scope, entry.PatientDisplayName), after, before),
            $"Edit surgery '{current.Name}'.",
            Surgery: surgery);
    }
}

/// <summary>Completes a planned surgery whose start has passed, or cancels a planned one, which notifies the patient and team.</summary>
public sealed class PrepareSurgeryStatusTool(
    string name,
    string actionType,
    string capability,
    bool cancels,
    ISurgeryToolchain surgeries,
    ISurgeryCatalogStore catalogs,
    ISurgeryAssistantProposalStore proposals,
    TimeProvider timeProvider) : SurgeryPreparationTool(
        name,
        actionType,
        capability,
        cancels
            ? "Prepare cancelling a planned surgery; the patient and team then receive the clinic's cancellation message."
            : "Prepare completing a planned surgery whose scheduled start has passed.",
        SurgeryAssistantSchemas.SurgeryId,
        proposals,
        timeProvider)
{
    private protected override async Task<SurgeryPreparationDraft> PrepareAsync(
        JsonElement arguments, SurgeryAssistantScope scope, Guid proposalId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        SurgeryCalendarEntry entry = await FindSurgeryAsync(surgeries, arguments, scope, cancellationToken);
        SurgeryReadModel current = entry.Surgery;
        if (cancels && !SurgeryPlanRules.IsCancellable(current.Status))
            throw new ArgumentException("Only a planned surgery can be cancelled; correct a mistaken completion by editing its status.");
        if (!cancels) SurgeryPlanRules.EnsureCompletable(current, now);
        SurgeryCatalog? type = await catalogs.FindAsync(current.CatalogId, cancellationToken);
        return new(
            Review(ReviewPatient(scope, entry.PatientDisplayName), SurgeryReviews.Plan(current, type?.Name), notifiesPatientAndTeam: cancels),
            $"{(cancels ? "Cancel" : "Complete")} surgery '{current.Name}' at {current.ScheduledAt:O}.",
            SurgeryId: current.Id,
            ExpectedConcurrencyToken: current.ConcurrencyToken);
    }
}

/// <summary>Creates a surgery type, or edits one; an edit keeps the omitted fields.</summary>
public sealed class PrepareSurgeryTypeTool(
    string name,
    string actionType,
    string capability,
    bool edits,
    ISurgeryCatalogStore catalogs,
    ISurgeryAssistantProposalStore proposals,
    TimeProvider timeProvider) : SurgeryPreparationTool(
        name,
        actionType,
        capability,
        edits
            ? "Prepare an edit of a surgery type; only the given fields change, and a list replaces the current one."
            : "Prepare a new surgery type whose tools, team roles, and reminders pre-fill new surgery plans.",
        edits ? SurgeryAssistantSchemas.TypeUpdate : SurgeryAssistantSchemas.TypeCreation,
        proposals,
        timeProvider)
{
    private protected override async Task<SurgeryPreparationDraft> PrepareAsync(
        JsonElement arguments, SurgeryAssistantScope scope, Guid proposalId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        SurgeryCatalog? current = null;
        Guid? typeId = null;
        if (edits)
        {
            typeId = SurgeryAssistantArguments.RequiredGuid(arguments, "surgeryTypeId");
            current = await FindTypeAsync(catalogs, typeId.Value.ToString("D"), cancellationToken);
        }

        SurgeryCatalogWriteModel type = new(
            SurgeryPlanRules.NormalizeText(SurgeryAssistantArguments.Text(arguments, "name")) ?? current?.Name ?? string.Empty,
            SurgeryAssistantArguments.Strings(arguments, "tools") ?? current?.Tools ?? [],
            SurgeryAssistantArguments.Strings(arguments, "defaultTeamRoles") ?? current?.DefaultTeamRoles ?? [],
            SurgeryAssistantArguments.Reminders(arguments, "reminderPolicies") ?? current?.ReminderPolicies ?? []);
        SurgeryPlanRules.ValidateCatalog(type);
        return new(
            Review(surgeryType: SurgeryReviews.Type(type), previousSurgeryType: current is null ? null : SurgeryReviews.Type(current)),
            edits ? $"Edit surgery type '{current!.Name}'." : $"Create surgery type '{type.Name}'.",
            SurgeryTypeId: typeId,
            ExpectedConcurrencyToken: current?.ConcurrencyToken ?? Guid.Empty,
            SurgeryType: type);
    }
}

/// <summary>Deletes a surgery type; surgeries already planned with it keep their values.</summary>
public sealed class PrepareSurgeryTypeDeletionTool(
    ISurgeryCatalogStore catalogs,
    ISurgeryAssistantProposalStore proposals,
    TimeProvider timeProvider) : SurgeryPreparationTool(
        SurgeryAssistantActions.PrepareTypeDeletionTool,
        SurgeryAssistantActions.DeleteType,
        SurgeryAssistantActions.OperationsCapability,
        "Prepare deleting a surgery type. The clinic's default type cannot be deleted.",
        SurgeryAssistantSchemas.TypeId,
        proposals,
        timeProvider)
{
    private protected override async Task<SurgeryPreparationDraft> PrepareAsync(
        JsonElement arguments, SurgeryAssistantScope scope, Guid proposalId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        Guid typeId = SurgeryAssistantArguments.RequiredGuid(arguments, "surgeryTypeId");
        SurgeryCatalog current = await FindTypeAsync(catalogs, typeId.ToString("D"), cancellationToken);
        return new(
            Review(previousSurgeryType: SurgeryReviews.Type(current)),
            $"Delete surgery type '{current.Name}'.",
            SurgeryTypeId: typeId,
            ExpectedConcurrencyToken: current.ConcurrencyToken);
    }
}

/// <summary>Changes the message the patient and team receive when a surgery is cancelled.</summary>
public sealed class PrepareSurgeryCancellationMessageTool(
    ISurgerySettingsStore settings,
    ISurgeryAssistantProposalStore proposals,
    TimeProvider timeProvider) : SurgeryPreparationTool(
        SurgeryAssistantActions.PrepareCancellationMessageTool,
        SurgeryAssistantActions.UpdateCancellationMessage,
        SurgeryAssistantActions.OperationsCapability,
        "Prepare a new cancellation message for the patient and surgical team.",
        SurgeryAssistantSchemas.CancellationMessage,
        proposals,
        timeProvider)
{
    private protected override async Task<SurgeryPreparationDraft> PrepareAsync(
        JsonElement arguments, SurgeryAssistantScope scope, Guid proposalId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        SurgerySettings current = await settings.GetAsync(cancellationToken)
            ?? throw new ArgumentException("The clinic has no cancellation message yet; open the surgery type settings once to create it.");
        SurgerySettingsWriteModel message = new(
            SurgeryAssistantArguments.RequiredText(arguments, "subject"),
            SurgeryAssistantArguments.RequiredText(arguments, "textBody"));
        SurgeryPlanRules.ValidateSettings(message);
        return new(
            Review(
                cancellationMessage: new(message.CancellationSubject, message.CancellationTextBody),
                previousCancellationMessage: new(current.CancellationSubject, current.CancellationTextBody)),
            "Change the surgery cancellation message.",
            ExpectedConcurrencyToken: current.ConcurrencyToken,
            CancellationMessage: message);
    }
}
