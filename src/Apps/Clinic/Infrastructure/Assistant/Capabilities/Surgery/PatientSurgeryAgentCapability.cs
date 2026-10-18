using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Assistant.Surgeries;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Surgery;

/// <summary>
/// The surgery workflow's capability and workflow keys, model-facing tool names, and persisted action types.
/// Action types are stored with prepared actions and read by the Web surgery review card: append, never rename.
/// </summary>
public static class SurgeryAssistantActions
{
    public const string Capability = "Surgeries";
    public const string OperationsCapability = "SurgeryOperations";
    public const string Workflow = "surgeries";

    public const string Create = "create_patient_surgery";
    public const string Update = "update_patient_surgery";
    public const string Complete = "complete_patient_surgery";
    public const string Cancel = "cancel_patient_surgery";
    public const string OperationsCreate = "create_surgery";
    public const string OperationsUpdate = "update_surgery";
    public const string OperationsComplete = "complete_surgery";
    public const string OperationsCancel = "cancel_surgery";
    public const string CreateType = "create_surgery_type";
    public const string UpdateType = "update_surgery_type";
    public const string DeleteType = "delete_surgery_type";
    public const string UpdateCancellationMessage = "update_surgery_cancellation_message";

    public const string ListTool = "list_patient_surgeries";
    public const string ListTypesTool = "list_surgery_types";
    public const string ListStaffTool = "list_surgery_staff";
    public const string ConflictsTool = "find_surgery_conflicts";
    public const string SuggestionsTool = "get_surgery_suggestions";
    public const string PrepareTool = "prepare_patient_surgery";
    public const string PrepareUpdateTool = "prepare_patient_surgery_update";
    public const string PrepareCompletionTool = "prepare_patient_surgery_completion";
    public const string PrepareCancellationTool = "prepare_patient_surgery_cancellation";

    public const string OperationsListTool = "list_operations_surgeries";
    public const string OperationsGetTool = "get_operations_surgery";
    public const string OperationsListTypesTool = "list_operations_surgery_types";
    public const string OperationsListStaffTool = "list_operations_surgery_staff";
    public const string OperationsConflictsTool = "find_operations_surgery_conflicts";
    public const string OperationsSuggestionsTool = "get_operations_surgery_suggestions";
    public const string GetCancellationMessageTool = "get_surgery_cancellation_message";
    public const string OperationsPrepareTool = "prepare_operations_surgery";
    public const string OperationsPrepareUpdateTool = "prepare_operations_surgery_update";
    public const string OperationsPrepareCompletionTool = "prepare_operations_surgery_completion";
    public const string OperationsPrepareCancellationTool = "prepare_operations_surgery_cancellation";
    public const string PrepareTypeCreationTool = "prepare_surgery_type_creation";
    public const string PrepareTypeUpdateTool = "prepare_surgery_type_update";
    public const string PrepareTypeDeletionTool = "prepare_surgery_type_deletion";
    public const string PrepareCancellationMessageTool = "prepare_surgery_cancellation_message_update";

    /// <summary>Every action type the surgery workflow commits.</summary>
    public static readonly IReadOnlySet<string> ActionTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        Create, Update, Complete, Cancel,
        OperationsCreate, OperationsUpdate, OperationsComplete, OperationsCancel,
        CreateType, UpdateType, DeleteType, UpdateCancellationMessage
    };

    /// <summary>Only the clinic owner can plan and manage surgeries, so only the owner is offered the preparations.</summary>
    internal static AssistantActionPolicy Prepare(
        string actionType, string toolName, AssistantActionRetryClassification retry, IEnumerable<string>? permissions = null, int revision = 1) =>
        AssistantActionPolicies.Prepare(
            actionType,
            Workflow,
            permissions ?? [],
            requiredRoles: [AssistantOperationsScope.TenantOwnerRole],
            toolNames: [toolName],
            revision: revision,
            retryClassification: retry);
}

/// <summary>The route-bound patient's surgeries: read them, and plan, edit, complete, or cancel them after confirmation.</summary>
public sealed class PatientSurgeryAgentCapability(
    ISurgeryToolchain surgeries,
    ISurgeryCatalogStore catalogs,
    ISurgeryTeamDirectory directory,
    IAssistantPatientSearch patients,
    ISurgeryAssistantProposalStore proposals,
    TimeProvider timeProvider) : AssistantCapability(
    [
        new ListPatientSurgeriesAssistantTool(surgeries),
        new ListSurgeryTypesAssistantTool(SurgeryAssistantActions.ListTypesTool, catalogs),
        new ListSurgeryStaffAssistantTool(SurgeryAssistantActions.ListStaffTool, directory),
        new FindSurgeryConflictsAssistantTool(SurgeryAssistantActions.ConflictsTool, surgeries),
        new GetSurgerySuggestionsAssistantTool(SurgeryAssistantActions.SuggestionsTool, surgeries),
        new PrepareSurgeryCreationTool(SurgeryAssistantActions.PrepareTool, SurgeryAssistantActions.Create, SurgeryAssistantActions.Capability,
            catalogs, directory, patients, proposals, timeProvider),
        new PrepareSurgeryUpdateTool(SurgeryAssistantActions.PrepareUpdateTool, SurgeryAssistantActions.Update, SurgeryAssistantActions.Capability,
            surgeries, catalogs, directory, proposals, timeProvider),
        new PrepareSurgeryStatusTool(SurgeryAssistantActions.PrepareCompletionTool, SurgeryAssistantActions.Complete, SurgeryAssistantActions.Capability,
            cancels: false, surgeries, catalogs, proposals, timeProvider),
        new PrepareSurgeryStatusTool(SurgeryAssistantActions.PrepareCancellationTool, SurgeryAssistantActions.Cancel, SurgeryAssistantActions.Capability,
            cancels: true, surgeries, catalogs, proposals, timeProvider)
    ],
    [
        // Revision 2 keeps its commit data on the server; a revision-1 proposal must be prepared again.
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.Create, SurgeryAssistantActions.PrepareTool,
            AssistantActionRetryClassification.Idempotent, ["ClinicalEvents.Create"], revision: 2),
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.Update, SurgeryAssistantActions.PrepareUpdateTool,
            AssistantActionRetryClassification.Retryable, ["ClinicalEvents.Create"]),
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.Complete, SurgeryAssistantActions.PrepareCompletionTool,
            AssistantActionRetryClassification.Retryable, ["ClinicalEvents.Create"]),
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.Cancel, SurgeryAssistantActions.PrepareCancellationTool,
            AssistantActionRetryClassification.Retryable, ["ClinicalEvents.Create"])
    ])
{
    public override string Name => SurgeryAssistantActions.Capability;
    public override string Description => "Read the current patient's surgeries and prepare planning, editing, completing, or cancelling them for explicit clinician confirmation.";
    public override IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { "ClinicalEvents.Read" };
    public override string GetInstructions() =>
        $"Use only the server-supplied patient scope. Before preparing a plan, call {SurgeryAssistantActions.ListTypesTool} for a valid surgery type and {SurgeryAssistantActions.ListStaffTool} for team user ids, " +
        $"and check the slot with {SurgeryAssistantActions.ConflictsTool}; a conflict is a warning to report, not a refusal. Use {SurgeryAssistantActions.ListTool} for surgery ids before editing, completing, or cancelling. " +
        "Reminders are sent automatically to the patient and every team member; cancelling also sends them the clinic's cancellation message. " +
        "Only a planned surgery can be cancelled; a mistaken completion is corrected by editing its status back to planned. " +
        "Confirm the full change with the clinician before preparing it; preparing never applies it.";
}

/// <summary>
/// The practice's surgeries across patients, its surgery types, and its cancellation message, for the clinic owner.
/// Patients are chosen through patient search tokens; no tool returns a patient id.
/// </summary>
public sealed class SurgeryOperationsAgentCapability(
    ISurgeryToolchain surgeries,
    ISurgeryCatalogStore catalogs,
    ISurgerySettingsStore settings,
    ISurgeryTeamDirectory directory,
    IAssistantPatientSearch patients,
    ISurgeryAssistantProposalStore proposals,
    TimeProvider timeProvider) : AssistantCapability(
    [
        new ListOperationsSurgeriesAssistantTool(surgeries),
        new GetOperationsSurgeryAssistantTool(surgeries),
        new ListSurgeryTypesAssistantTool(SurgeryAssistantActions.OperationsListTypesTool, catalogs),
        new ListSurgeryStaffAssistantTool(SurgeryAssistantActions.OperationsListStaffTool, directory),
        new FindSurgeryConflictsAssistantTool(SurgeryAssistantActions.OperationsConflictsTool, surgeries),
        new GetSurgerySuggestionsAssistantTool(SurgeryAssistantActions.OperationsSuggestionsTool, surgeries),
        new GetSurgeryCancellationMessageAssistantTool(settings),
        new PrepareSurgeryCreationTool(SurgeryAssistantActions.OperationsPrepareTool, SurgeryAssistantActions.OperationsCreate, SurgeryAssistantActions.OperationsCapability,
            catalogs, directory, patients, proposals, timeProvider),
        new PrepareSurgeryUpdateTool(SurgeryAssistantActions.OperationsPrepareUpdateTool, SurgeryAssistantActions.OperationsUpdate, SurgeryAssistantActions.OperationsCapability,
            surgeries, catalogs, directory, proposals, timeProvider),
        new PrepareSurgeryStatusTool(SurgeryAssistantActions.OperationsPrepareCompletionTool, SurgeryAssistantActions.OperationsComplete, SurgeryAssistantActions.OperationsCapability,
            cancels: false, surgeries, catalogs, proposals, timeProvider),
        new PrepareSurgeryStatusTool(SurgeryAssistantActions.OperationsPrepareCancellationTool, SurgeryAssistantActions.OperationsCancel, SurgeryAssistantActions.OperationsCapability,
            cancels: true, surgeries, catalogs, proposals, timeProvider),
        new PrepareSurgeryTypeTool(SurgeryAssistantActions.PrepareTypeCreationTool, SurgeryAssistantActions.CreateType, SurgeryAssistantActions.OperationsCapability,
            edits: false, catalogs, proposals, timeProvider),
        new PrepareSurgeryTypeTool(SurgeryAssistantActions.PrepareTypeUpdateTool, SurgeryAssistantActions.UpdateType, SurgeryAssistantActions.OperationsCapability,
            edits: true, catalogs, proposals, timeProvider),
        new PrepareSurgeryTypeDeletionTool(catalogs, proposals, timeProvider),
        new PrepareSurgeryCancellationMessageTool(settings, proposals, timeProvider)
    ],
    [
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.OperationsCreate, SurgeryAssistantActions.OperationsPrepareTool, AssistantActionRetryClassification.Idempotent),
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.OperationsUpdate, SurgeryAssistantActions.OperationsPrepareUpdateTool, AssistantActionRetryClassification.Retryable),
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.OperationsComplete, SurgeryAssistantActions.OperationsPrepareCompletionTool, AssistantActionRetryClassification.Retryable),
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.OperationsCancel, SurgeryAssistantActions.OperationsPrepareCancellationTool, AssistantActionRetryClassification.Retryable),
        // A new type has no idempotency key, so a failed creation is never retried automatically.
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.CreateType, SurgeryAssistantActions.PrepareTypeCreationTool, AssistantActionRetryClassification.NeverRetry),
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.UpdateType, SurgeryAssistantActions.PrepareTypeUpdateTool, AssistantActionRetryClassification.Retryable),
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.DeleteType, SurgeryAssistantActions.PrepareTypeDeletionTool, AssistantActionRetryClassification.Retryable),
        SurgeryAssistantActions.Prepare(SurgeryAssistantActions.UpdateCancellationMessage, SurgeryAssistantActions.PrepareCancellationMessageTool, AssistantActionRetryClassification.Retryable)
    ])
{
    public override string Name => SurgeryAssistantActions.OperationsCapability;
    public override string Description => "Read the practice's surgery calendar and prepare planning, editing, completing, or cancelling surgeries, managing surgery types, and changing the cancellation message for explicit owner confirmation.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public override bool IsAvailable(AssistantContext context) =>
        context.SubjectKind == SurgeryAssistantScope.PracticeSubject && AssistantOperationsScope.IsAuthorizedRole(context.Role);
    public override string GetInstructions() =>
        $"Find the patient with search_patients and pass its candidate token to {SurgeryAssistantActions.OperationsPrepareTool}. Use {SurgeryAssistantActions.OperationsListTool} or {SurgeryAssistantActions.OperationsGetTool} for surgery ids, " +
        $"{SurgeryAssistantActions.OperationsListTypesTool} for type ids, and {SurgeryAssistantActions.OperationsListStaffTool} for team user ids; check a slot with {SurgeryAssistantActions.OperationsConflictsTool} and report conflicts as warnings. " +
        "Only a planned surgery can be cancelled, which sends the patient and team the cancellation message; a mistaken completion is corrected by editing its status back to planned. " +
        "Every change is prepared for the owner's explicit confirmation; never claim it was applied during the run.";
}
