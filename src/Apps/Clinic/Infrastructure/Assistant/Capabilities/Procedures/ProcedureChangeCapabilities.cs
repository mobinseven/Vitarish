using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Vitarish.Communications.Domain.Enums;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;
using Names = Vitarish.Clinic.Application.Assistant.Presentation.ClinicPresentableToolNames;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Procedures;

/// <summary>
/// Patient page: prepare changes to the current patient's procedures and steps. Each change is a proposal the person
/// confirms; the workflow's own authorization decides at preparation and again at confirmation (ADR-0023 decision 6).
/// </summary>
public sealed class ProcedureChangeCapability(PatientProcedureRecords records, ISeaManagementAgentTool workflow)
    : AssistantCapability(
        ProcedureChangeTools.Create(records, workflow, ProcedureToolWorkspace.Patient),
        ProcedureChangeTools.Policies(ProcedureToolWorkspace.Patient, ["ClinicalEvents.Read"]))
{
    public override string Name => ProcedureCapabilityNames.Changes;
    public override string Description => "Prepare committing or reopening a step, adding a note or a reminder to a step, and adding a procedure for the current patient.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal) { "ClinicalEvents.Read" };
    public override string GetInstructions() => ProcedureChangeTools.Instructions;
}

/// <summary>Practice workspace: the same changes for a patient found by patient search in this turn.</summary>
public sealed class OperationsProcedureChangeCapability(PatientProcedureRecords records, ISeaManagementAgentTool workflow)
    : AssistantCapability(
        ProcedureChangeTools.Create(records, workflow, ProcedureToolWorkspace.Operations),
        ProcedureChangeTools.Policies(ProcedureToolWorkspace.Operations, []))
{
    public override string Name => ProcedureCapabilityNames.OperationsChanges;
    public override string Description => "Prepare committing or reopening a step, adding a note or a reminder to a step, and adding a procedure for a searched patient.";
    public override IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public override bool IsAvailable(AssistantContext context) =>
        context.SubjectKind == "practice" && AssistantOperationsScope.IsAuthorizedRole(context.Role);
    public override string GetInstructions() => "Pass the candidate token from this turn's patient search. " + ProcedureChangeTools.Instructions;
}

internal static class ProcedureChangeTools
{
    public const string Instructions =
        "Every change is only a proposal shown to the user for confirmation; never say it is done before they confirm. " +
        "Read the step first so the proposal names the right step. A not_permitted result means the user's access does not allow the change: say so.";

    public static IReadOnlyCollection<IAgentTool> Create(PatientProcedureRecords records, ISeaManagementAgentTool workflow, ProcedureToolWorkspace workspace) =>
    [
        new StepStatusTool(records, workflow, workspace),
        new StepNoteTool(records, workflow, workspace),
        new StepReminderTool(records, workflow, workspace),
        new ProcedureCreateTool(records, workflow, workspace)
    ];

    public static IReadOnlyCollection<AssistantActionPolicy> Policies(ProcedureToolWorkspace workspace, IEnumerable<string> permissions)
    {
        bool operations = workspace == ProcedureToolWorkspace.Operations;
        return
        [
            AssistantActionPolicies.Prepare(
                operations ? ProcedureActionTypes.OperationsStepChange : ProcedureActionTypes.StepChange,
                ProcedureActionTypes.Workflow, permissions,
                operations ? [AssistantOperationsScope.TenantOwnerRole] : null,
                operations
                    ? [Names.PrepareOperationsStepStatus, Names.PrepareOperationsStepNote, Names.PrepareOperationsStepReminder]
                    : [Names.PrepareStepStatus, Names.PrepareStepNote, Names.PrepareStepReminder]),
            AssistantActionPolicies.Prepare(
                operations ? ProcedureActionTypes.OperationsCreate : ProcedureActionTypes.Create,
                ProcedureActionTypes.Workflow, permissions,
                [AssistantOperationsScope.TenantOwnerRole],
                [operations ? Names.PrepareOperationsPatientProcedure : Names.PreparePatientProcedure])
        ];
    }
}

/// <summary>
/// A prepared change as the model reads it. <see cref="Confirmation"/> is the workflow's prepared action, encoded so
/// the model reads an opaque token rather than workflow terms; it is decoded into the stored review payload.
/// </summary>
public sealed record ProcedureChangeResult(string Status, ProcedureChangeReview? Change = null, string? Confirmation = null, string? Message = null)
{
    public const string Prepared = "prepared";
    public const string NotPermitted = "not_permitted";
    public const string NotFound = "not_found";
}

/// <summary>What the person reviews: server-resolved names and the exact content that will be written.</summary>
public sealed record ProcedureChangeReview(
    string Kind,
    string? ProcedureName = null,
    string? StepTitle = null,
    int? StepPosition = null,
    string? Text = null,
    DateTimeOffset? RemindAt = null,
    string? Channel = null,
    string? Description = null);

/// <summary>The stored review payload of a procedure change: the review and the workflow's confirmation.</summary>
public sealed record ProcedureChangePayload(ProcedureChangeReview Change, SeaManagementPreparedAction Confirmation);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ProcedureChangeResult))]
[JsonSerializable(typeof(ProcedureChangePayload))]
[JsonSerializable(typeof(SeaManagementPreparedAction))]
internal sealed partial class ProcedureChangeJsonSerializerContext : JsonSerializerContext;

internal abstract class ProcedureChangeTool(PatientProcedureRecords records, ISeaManagementAgentTool workflow, ProcedureToolWorkspace workspace)
    : IAgentTool, IAssistantPreparationTool
{
    protected PatientProcedureRecords Records { get; } = records;
    protected ProcedureToolWorkspace Workspace { get; } = workspace;
    protected bool IsOperations => Workspace == ProcedureToolWorkspace.Operations;
    protected abstract string ActionType { get; }
    protected abstract string CapabilityName { get; }
    public abstract AgentToolDefinition Definition { get; }

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        Guid patientId = await Records.ResolvePatientAsync(Workspace, document.RootElement, context, cancellationToken);
        ProcedureChangeResult result;
        try
        {
            result = await PrepareAsync(call, document.RootElement, patientId, context, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            // The workflow refused: the person's access does not allow this change. The model tells them so.
            result = new(ProcedureChangeResult.NotPermitted, Message: "The user's access does not allow this change.");
        }

        return new(call.Id, JsonSerializer.Serialize(result, ProcedureChangeJsonSerializerContext.Default.ProcedureChangeResult));
    }

    public AssistantPreparedAction? GetPreparedAction(AgentToolResult result)
    {
        ProcedureChangeResult? change = JsonSerializer.Deserialize(result.Output, ProcedureChangeJsonSerializerContext.Default.ProcedureChangeResult);
        return change is { Status: ProcedureChangeResult.Prepared, Change: { } review, Confirmation: { } token } && Decode(token) is { } confirmation
            ? new(CapabilityName, ActionType, confirmation.ActionId,
                JsonSerializer.Serialize(new ProcedureChangePayload(review, confirmation), ProcedureChangeJsonSerializerContext.Default.ProcedureChangePayload),
                confirmation.ExpiresAt)
            : null;
    }

    protected abstract Task<ProcedureChangeResult> PrepareAsync(AgentToolCall call, JsonElement arguments, Guid patientId, AgentExecutionContext context, CancellationToken ct);

    /// <summary>Asks the workflow to prepare <paramref name="operation"/>; it authorizes and returns the confirmation to store.</summary>
    protected async Task<ProcedureChangeResult> PrepareWithWorkflowAsync(AgentToolCall call, string operation, JsonObject payload, ProcedureChangeReview review, AgentExecutionContext context, CancellationToken ct)
    {
        string arguments = new JsonObject { [SeaManagementOperations.Operation] = operation, [SeaManagementOperations.Payload] = payload }.ToJsonString();
        AgentToolResult prepared = await workflow.ExecuteAsync(new(call.Id, call.Name, arguments), context, ct);
        SeaManagementPreparedAction confirmation = workflow.ParsePreparedAction(prepared)
            ?? throw new InvalidOperationException("The workflow did not prepare the change.");
        return new(ProcedureChangeResult.Prepared, review, Encode(confirmation));
    }

    private static string Encode(SeaManagementPreparedAction confirmation) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(confirmation, ProcedureChangeJsonSerializerContext.Default.SeaManagementPreparedAction));

    private static SeaManagementPreparedAction? Decode(string token)
    {
        try { return JsonSerializer.Deserialize(Convert.FromBase64String(token), ProcedureChangeJsonSerializerContext.Default.SeaManagementPreparedAction); }
        catch (FormatException) { return null; }
        catch (JsonException) { return null; }
    }

    protected string Name(string patientTool, string operationsTool) => IsOperations ? operationsTool : patientTool;
    protected string ChangeAction(bool create) =>
        create ? (IsOperations ? ProcedureActionTypes.OperationsCreate : ProcedureActionTypes.Create)
            : (IsOperations ? ProcedureActionTypes.OperationsStepChange : ProcedureActionTypes.StepChange);
    protected string Capability => IsOperations ? ProcedureCapabilityNames.OperationsChanges : ProcedureCapabilityNames.Changes;

    protected static string RequiredText(JsonElement arguments, string name, int maxLength)
    {
        string text = arguments.TryGetProperty(name, out JsonElement value) && value.GetString() is { } raw ? raw.Trim() : string.Empty;
        return text.Length switch
        {
            0 => throw new JsonException($"{name} is required."),
            _ when text.Length > maxLength => throw new JsonException($"{name} must be at most {maxLength} characters."),
            _ => text
        };
    }

    /// <summary>The step to change, with its procedure name and position; null when it is not this patient's or hidden.</summary>
    protected async Task<(EventReadDto Step, string? ProcedureName, int Position)?> StepAsync(JsonElement arguments, Guid patientId, AgentExecutionContext context, CancellationToken ct)
    {
        StepDetailResponse detail = await Records.GetStepAsync(patientId, ProcedureReadTools.Id(arguments, "stepId"), context, ct);
        if (detail.Step is not { } step) return null;
        (EventReadDto? record, _) = await Records.StepAsync(patientId, step.Id, context, ct);
        return record is null ? null : (record, step.ProcedureName, step.Position);
    }
}

internal sealed class StepStatusTool(PatientProcedureRecords records, ISeaManagementAgentTool workflow, ProcedureToolWorkspace workspace)
    : ProcedureChangeTool(records, workflow, workspace)
{
    protected override string ActionType => ChangeAction(false);
    protected override string CapabilityName => Capability;
    public override AgentToolDefinition Definition { get; } = new(
        workspace == ProcedureToolWorkspace.Operations ? Names.PrepareOperationsStepStatus : Names.PrepareStepStatus,
        "Prepare marking one of the patient's steps committed (done) or reopening it (open again), for the user to confirm.",
        ProcedureReadTools.Schema(workspace, "\"stepId\":{\"type\":\"string\"},\"status\":{\"type\":\"string\",\"enum\":[\"committed\",\"open\"]}", "stepId", "status"));

    protected override async Task<ProcedureChangeResult> PrepareAsync(AgentToolCall call, JsonElement arguments, Guid patientId, AgentExecutionContext context, CancellationToken ct)
    {
        if (await StepAsync(arguments, patientId, context, ct) is not { } target) return new(ProcedureChangeResult.NotFound, Message: "No such step of this patient that the user can open.");
        bool commit = arguments.GetProperty("status").GetString() == "committed";
        ProcedureChangeReview review = new(commit ? ProcedureChangeKinds.CommitStep : ProcedureChangeKinds.ReopenStep, target.ProcedureName, target.Step.Title, target.Position);
        return await PrepareWithWorkflowAsync(call, commit ? SeaManagementOperations.CommitEvent : SeaManagementOperations.UncommitEvent,
            new JsonObject { ["id"] = target.Step.Id }, review, context, ct);
    }
}

internal sealed class StepNoteTool(PatientProcedureRecords records, ISeaManagementAgentTool workflow, ProcedureToolWorkspace workspace)
    : ProcedureChangeTool(records, workflow, workspace)
{
    public const int MaxLength = 4000;
    /// <summary>The content type the step screen gives a typed note.</summary>
    private const string NoteContentType = "text/plain";
    protected override string ActionType => ChangeAction(false);
    protected override string CapabilityName => Capability;
    public override AgentToolDefinition Definition { get; } = new(
        workspace == ProcedureToolWorkspace.Operations ? Names.PrepareOperationsStepNote : Names.PrepareStepNote,
        "Prepare adding a text note to one of the patient's steps, worded as the user dictated it, for the user to confirm.",
        ProcedureReadTools.Schema(workspace, $"\"stepId\":{{\"type\":\"string\"}},\"text\":{{\"type\":\"string\",\"maxLength\":{MaxLength}}}", "stepId", "text"));

    protected override async Task<ProcedureChangeResult> PrepareAsync(AgentToolCall call, JsonElement arguments, Guid patientId, AgentExecutionContext context, CancellationToken ct)
    {
        string text = RequiredText(arguments, "text", MaxLength);
        if (await StepAsync(arguments, patientId, context, ct) is not { } target) return new(ProcedureChangeResult.NotFound, Message: "No such step of this patient that the user can open.");
        ProcedureChangeReview review = new(ProcedureChangeKinds.AddNote, target.ProcedureName, target.Step.Title, target.Position, text);
        return await PrepareWithWorkflowAsync(call, SeaManagementOperations.CreateAttachment,
            new JsonObject
            {
                ["eventId"] = target.Step.Id,
                ["content"] = text,
                ["contentType"] = NoteContentType,
                ["size"] = Encoding.UTF8.GetByteCount(text),
                ["kind"] = AttachmentKind.Text.ToString()
            },
            review, context, ct);
    }
}

internal sealed class StepReminderTool(PatientProcedureRecords records, ISeaManagementAgentTool workflow, ProcedureToolWorkspace workspace)
    : ProcedureChangeTool(records, workflow, workspace)
{
    public const int MaxLength = 500;
    private static bool HasOffset(string value) =>
        value.EndsWith('Z') || value.EndsWith('z') || (value.Length > 6 && value[^6] is '+' or '-' && value[^3] == ':');
    private const string EmailChannel = "email";
    private const string SmsChannel = "sms";
    protected override string ActionType => ChangeAction(false);
    protected override string CapabilityName => Capability;
    public override AgentToolDefinition Definition { get; } = new(
        workspace == ProcedureToolWorkspace.Operations ? Names.PrepareOperationsStepReminder : Names.PrepareStepReminder,
        "Prepare a reminder about one of the patient's steps, sent to the signed-in user at a time by SMS or email, for the user to confirm. remindAt is an ISO 8601 date-time with offset.",
        ProcedureReadTools.Schema(workspace,
            $"\"stepId\":{{\"type\":\"string\"}},\"remindAt\":{{\"type\":\"string\"}},\"channel\":{{\"type\":\"string\",\"enum\":[\"sms\",\"email\"]}},\"message\":{{\"type\":\"string\",\"maxLength\":{MaxLength}}}",
            "stepId", "remindAt", "channel", "message"));

    protected override async Task<ProcedureChangeResult> PrepareAsync(AgentToolCall call, JsonElement arguments, Guid patientId, AgentExecutionContext context, CancellationToken ct)
    {
        string message = RequiredText(arguments, "message", MaxLength);
        string remindAtText = RequiredText(arguments, "remindAt", 40);
        // A time without an offset would be read in the server's zone; the user's intent must carry its own offset.
        if (!HasOffset(remindAtText) || !DateTimeOffset.TryParse(remindAtText, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset remindAt))
            throw new JsonException("remindAt must be an ISO 8601 date-time with an offset, such as 2030-01-12T09:00:00+03:30.");
        string channel = arguments.GetProperty("channel").GetString() == EmailChannel ? EmailChannel : SmsChannel;
        if (await StepAsync(arguments, patientId, context, ct) is not { } target) return new(ProcedureChangeResult.NotFound, Message: "No such step of this patient that the user can open.");
        ProcedureChangeReview review = new(ProcedureChangeKinds.AddReminder, target.ProcedureName, target.Step.Title, target.Position, message, remindAt, channel);
        string userId = PatientProcedureRecords.Subject(context).UserId;
        return await PrepareWithWorkflowAsync(call, SeaManagementOperations.CreateReminder,
            new JsonObject
            {
                ["sourceId"] = target.Step.Id,
                ["recipientUserId"] = userId,
                ["kind"] = (channel == EmailChannel ? ReminderKind.Email : ReminderKind.Sms).ToString(),
                ["remindAt"] = remindAt.ToString("O"),
                ["subject"] = target.Step.Title,
                ["textBody"] = message
            },
            review, context, ct);
    }
}

internal sealed class ProcedureCreateTool(PatientProcedureRecords records, ISeaManagementAgentTool workflow, ProcedureToolWorkspace workspace)
    : ProcedureChangeTool(records, workflow, workspace)
{
    public const int MaxNameLength = 200;
    public const int MaxDescriptionLength = 2000;
    protected override string ActionType => ChangeAction(true);
    protected override string CapabilityName => Capability;
    public override AgentToolDefinition Definition { get; } = new(
        workspace == ProcedureToolWorkspace.Operations ? Names.PrepareOperationsPatientProcedure : Names.PreparePatientProcedure,
        "Prepare adding a new empty procedure for the patient, with a name and optional details, for the user to confirm. Steps are added afterwards.",
        ProcedureReadTools.Schema(workspace,
            $"\"name\":{{\"type\":\"string\",\"maxLength\":{MaxNameLength}}},\"description\":{{\"type\":\"string\",\"maxLength\":{MaxDescriptionLength}}}", "name"));

    protected override async Task<ProcedureChangeResult> PrepareAsync(AgentToolCall call, JsonElement arguments, Guid patientId, AgentExecutionContext context, CancellationToken ct)
    {
        string name = RequiredText(arguments, "name", MaxNameLength);
        string? description = arguments.TryGetProperty("description", out JsonElement value) && value.GetString() is { Length: > 0 } text
            ? text.Trim()[..Math.Min(text.Trim().Length, MaxDescriptionLength)]
            : null;
        ProcedureChangeReview review = new(ProcedureChangeKinds.AddProcedure, ProcedureName: name, Description: description);
        // The workflow reads a procedure in its own write shape, whose member names are PascalCase.
        return await PrepareWithWorkflowAsync(call, SeaManagementOperations.CreateProcedure,
            new JsonObject { [nameof(ProcedureWriteDto.ProjectId)] = patientId, [nameof(ProcedureWriteDto.Name)] = name, [nameof(ProcedureWriteDto.Description)] = description },
            review, context, ct);
    }
}
