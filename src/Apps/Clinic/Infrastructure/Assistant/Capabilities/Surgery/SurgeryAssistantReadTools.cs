using System.Text.Json;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Surgery;

// Read tools of the surgery workflow. The tools both assistants use take their name at construction,
// because tool names are unique across assistants.

public sealed class ListPatientSurgeriesAssistantTool(ISurgeryToolchain surgeries) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new(
        SurgeryAssistantActions.ListTool,
        "List surgeries for the current patient, any status, with their surgery ids.",
        SurgeryAssistantSchemas.Empty);

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SurgeryReadModel> values = await surgeries.GetByPatientAsync(SurgeryAssistantScope.PatientIdOf(context), cancellationToken);
        return new(call.Id, SurgeryAssistantJsonSerializerContext.Serialize(new SurgeryAssistantListResult(values)));
    }
}

/// <summary>The clinic calendar's surgeries in a bounded range, without patient ids.</summary>
public sealed class ListOperationsSurgeriesAssistantTool(ISurgeryToolchain surgeries) : IAgentTool
{
    private static readonly TimeSpan MaxRange = TimeSpan.FromDays(93);

    public AgentToolDefinition Definition { get; } = new(
        SurgeryAssistantActions.OperationsListTool,
        "List the authenticated practice's surgeries starting in a date range, optionally of one status, ordered by start.",
        SurgeryAssistantSchemas.Calendar);

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        JsonElement arguments = document.RootElement;
        DateTimeOffset from = SurgeryAssistantArguments.Instant(arguments, "from") ?? throw new ArgumentException("from is required.", "from");
        DateTimeOffset to = SurgeryAssistantArguments.Instant(arguments, "to") ?? throw new ArgumentException("to is required.", "to");
        if (to - from > MaxRange) throw new ArgumentException("The range can cover at most 93 days.", "to");
        SurgeryStatus? status = SurgeryAssistantArguments.Text(arguments, "status") is { } text
            ? Enum.Parse<SurgeryStatus>(text, ignoreCase: true)
            : null;
        IReadOnlyList<SurgeryCalendarEntry> entries = await surgeries.GetCalendarAsync(from, to, cancellationToken);
        return new(call.Id, SurgeryAssistantJsonSerializerContext.Serialize(new SurgeryOperationsListResult(entries
            .Where(entry => status is null || entry.Surgery.Status == status)
            .Select(SurgeryOperationsView.From)
            .ToArray())));
    }
}

public sealed class GetOperationsSurgeryAssistantTool(ISurgeryToolchain surgeries) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new(
        SurgeryAssistantActions.OperationsGetTool,
        "Get one surgery of the authenticated practice by its surgery id.",
        SurgeryAssistantSchemas.SurgeryId);

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        Guid surgeryId = SurgeryAssistantArguments.RequiredGuid(document.RootElement, "surgeryId");
        SurgeryCalendarEntry entry = await surgeries.GetAsync(surgeryId, cancellationToken)
            ?? throw new ArgumentException("The surgery does not exist.", "surgeryId");
        return new(call.Id, SurgeryAssistantJsonSerializerContext.Serialize(SurgeryOperationsView.From(entry)));
    }
}

public sealed class ListSurgeryTypesAssistantTool(string name, ISurgeryCatalogStore catalogs) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new(
        name,
        "List this clinic's surgery types with their ids, default tools, team roles and reminders.",
        SurgeryAssistantSchemas.Empty);

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SurgeryCatalog> types = await catalogs.ListAsync(cancellationToken);
        return new(call.Id, SurgeryAssistantJsonSerializerContext.Serialize(new SurgeryAssistantTypesResult(types)));
    }
}

/// <summary>Clinic staff who can join a surgery team: user ids and names only, never contact details.</summary>
public sealed class ListSurgeryStaffAssistantTool(string name, ISurgeryTeamDirectory directory) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new(
        name,
        "List clinic staff who can join a surgery team, optionally matching a name; use their user ids for the team.",
        SurgeryAssistantSchemas.Staff);

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        IReadOnlyList<SurgeryTeamContact> members = await directory.SearchMembersAsync(
            SurgeryAssistantArguments.Text(document.RootElement, "search"), cancellationToken);
        return new(call.Id, SurgeryAssistantJsonSerializerContext.Serialize(new SurgeryStaffResult(
            members.Select(member => new SurgeryStaffView(member.UserId, member.DisplayName)).ToArray())));
    }
}

/// <summary>Planned surgeries that would overlap a time slot in the same location or with a shared team member.</summary>
public sealed class FindSurgeryConflictsAssistantTool(string name, ISurgeryToolchain surgeries) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new(
        name,
        "Check a surgery time slot against other planned surgeries in the same location or with a shared team member. A conflict is a warning; the plan can still be saved.",
        SurgeryAssistantSchemas.Conflicts);

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        using JsonDocument document = JsonDocument.Parse(call.ArgumentsJson);
        JsonElement arguments = document.RootElement;
        bool namesPatients = SurgeryAssistantScope.From(context).IsPatient is false;
        IReadOnlyList<SurgeryConflict> conflicts = await surgeries.FindConflictsAsync(new(
            SurgeryAssistantArguments.Instant(arguments, "scheduledAt") ?? throw new ArgumentException("scheduledAt is required.", "scheduledAt"),
            SurgeryAssistantArguments.Integer(arguments, "durationMinutes"),
            SurgeryAssistantArguments.Text(arguments, "location"),
            SurgeryAssistantArguments.Strings(arguments, "teamUserIds") ?? [],
            SurgeryAssistantArguments.OptionalGuid(arguments, "excludeSurgeryId")), cancellationToken);
        return new(call.Id, SurgeryAssistantJsonSerializerContext.Serialize(new SurgeryConflictsResult(conflicts.Select(conflict => new SurgeryConflictView(
            conflict.Entry.Surgery.Id,
            conflict.Entry.Surgery.Name,
            namesPatients ? conflict.Entry.PatientDisplayName : null,
            conflict.Entry.Surgery.ScheduledAt,
            conflict.Entry.Surgery.DurationMinutes,
            conflict.Entry.Surgery.Location,
            conflict.Reasons,
            conflict.SharedTeamUserIds)).ToArray())));
    }
}

public sealed class GetSurgerySuggestionsAssistantTool(string name, ISurgeryToolchain surgeries) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new(
        name,
        "List locations and team roles used in earlier surgery plans, most used first.",
        SurgeryAssistantSchemas.Empty);

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
        new(call.Id, SurgeryAssistantJsonSerializerContext.Serialize(await surgeries.GetSuggestionsAsync(cancellationToken)));
}

public sealed class GetSurgeryCancellationMessageAssistantTool(ISurgerySettingsStore settings) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new(
        SurgeryAssistantActions.GetCancellationMessageTool,
        "Get the message the patient and surgical team receive when a surgery is cancelled.",
        SurgeryAssistantSchemas.Empty);

    public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        SurgerySettings? current = await settings.GetAsync(cancellationToken);
        return new(call.Id, SurgeryAssistantJsonSerializerContext.Serialize(new SurgeryCancellationMessageResult(
            current is null ? null : new(current.CancellationSubject, current.CancellationTextBody))));
    }
}
