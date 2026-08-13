using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;
using Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;
using Vitarish.Clinic.Application.ClinicWorkspace.Operations;
using Vitarish.Intelligence.Agents;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class ClinicAssistantRuntime(
    AgentProfileComposer<ClinicAssistantCompositionContext> profileComposer,
    ClinicWorkspaceCapabilityRegistry capabilityRegistry,
    ClinicWorkspaceToolDispatcher toolDispatcher,
    ClinicAssistantOptions options,
    IEnumerable<IIntelligenceAgentTool> intelligenceTools,
    ISender sender,
    IClinicWorkspaceActionStore actionStore,
    TimeProvider timeProvider) : IClinicWorkspaceRuntime, IClinicWorkspaceGuardedToolResolver
{
    public const string ResultSchema = """
        {
          "type":"object",
          "additionalProperties":false,
          "required":["messages","actions"],
          "properties":{
            "messages":{"type":"array","items":{"type":"object","additionalProperties":false,"required":["kind","content"],"properties":{"kind":{"type":"string","enum":["assistantMessage","informationalToolResult","missingInformationRequest","failure"]},"content":{"type":"string"}}}},
            "actions":{"type":"array","items":{"type":"object","additionalProperties":false,"required":["capability","actionType","summary"],"properties":{"capability":{"type":"string"},"actionType":{"type":"string"},"summary":{"type":"string"}}}}
          }
        }
        """;

    public async Task<ClinicWorkspaceResult> RunAsync(
        ClinicWorkspaceContext context,
        ClinicWorkspaceInput input,
        CancellationToken cancellationToken = default)
    {
        ClinicAssistantManifest workspace = Compose(context);
        if (string.IsNullOrWhiteSpace(input.Message))
            return ClinicWorkspaceResult.Failure(workspace.ExecutionContext.ExecutionId, context.ConversationId, "A message is required.");

        Guid? existingConversationId = context.IsNewConversation
            ? null
            : Guid.TryParse(context.ConversationId, out Guid parsedConversationId)
                ? parsedConversationId
                : Guid.Empty;
        ClinicWorkspaceResult? parsedResult = null;
        string agentKey = AgentKey(context);
        string subjectId = SubjectId(context);
        RunAgentConversationResult conversation = await sender.Send(new RunAgentConversation(
            existingConversationId,
            new AgentConversationScope(context.PracticeId, context.AuthenticatedUserId, agentKey, context.SubjectKind, subjectId),
            workspace.Definition,
            new AgentInput(input.Message),
            workspace.Tools,
            workspace.ExecutionContext,
            workspace.Limits,
            async (conversationId, run, callbackCancellationToken) =>
            {
                try
                {
                    if (run.StructuredOutput is not JsonElement output)
                        return AgentConversationOutcome.InvalidHistory;
                    if (run.ToolExecutions.Count > 0)
                    {
                        workspace.ExecutionCollector.RecordRemote(
                            run.ToolExecutions,
                            workspace.Tools,
                            workspace.ToolCapabilities,
                            workspace.ExecutionContext);
                    }

                    ClinicWorkspaceContext persistedContext = context with
                    {
                        ConversationId = conversationId.ToString("N"),
                        IsNewConversation = false
                    };
                    parsedResult = Parse(output, workspace, persistedContext, timeProvider.GetUtcNow(), run.ToolExecutions);
                    if (parsedResult.PreparedActions.Count > 0)
                    {
                        IReadOnlyList<ClinicWorkspaceAction> actions = await PersistActionsAsync(
                            parsedResult, persistedContext, cancellationToken: callbackCancellationToken);
                        parsedResult = parsedResult with { Actions = actions, PreparedActions = [] };
                    }
                    return AgentConversationOutcome.Succeeded;
                }
                catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
                {
                    return AgentConversationOutcome.InvalidHistory;
                }
            },
            ProfileCompatibilityKey: IsLegacyAgentKey(context.AgentKey) ? null : workspace.CompatibilityKey), cancellationToken);

        AgentRunResult? run = conversation.Run;
        if (conversation.Outcome != AgentConversationOutcome.Succeeded ||
            run is null ||
            !run.IsSuccess ||
            run.StructuredOutput is not JsonElement)
        {
            ClinicWorkspaceResult failure = ClinicWorkspaceResult.Failure(
                workspace.ExecutionContext.ExecutionId,
                conversation.ConversationId?.ToString("N") ?? context.ConversationId,
                run?.Error?.Message ?? "The clinic assistant did not return a valid result.");
            return failure with
            {
                Outcome = conversation.Outcome switch
                {
                    AgentConversationOutcome.NotFound => ClinicWorkspaceRunOutcome.NotFound,
                    AgentConversationOutcome.Busy => ClinicWorkspaceRunOutcome.Busy,
                    AgentConversationOutcome.ProfileChanged => ClinicWorkspaceRunOutcome.ProfileChanged,
                    _ => ClinicWorkspaceRunOutcome.Failed
                }
            };
        }

        return parsedResult is null
            ? ClinicWorkspaceResult.Failure(workspace.ExecutionContext.ExecutionId, context.ConversationId, "The clinic assistant returned an invalid structured result.")
            : parsedResult with { ConversationId = conversation.ConversationId!.Value.ToString("N") };
    }

    public async Task<ClinicWorkspaceToolResult> ResolveAsync(
        ClinicWorkspaceToolResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        AgentExecutionContext execution = new(request.ExecutionId, request.ExecutionMetadata);
        ClinicAssistantManifest workspace = Compose(request.Context, execution);
        IAgentTool? tool = workspace.Tools.FirstOrDefault(candidate =>
            string.Equals(candidate.Definition.Name, request.ToolName, StringComparison.Ordinal));
        if (tool is null)
            throw new KeyNotFoundException();

        AgentToolResult result = await tool.ExecuteAsync(
            new AgentToolCall(request.ToolCallId, request.ToolName, request.ArgumentsJson),
            execution,
            cancellationToken);
        return new(result.CallId, result.Output, result.IsError);
    }

    private ClinicAssistantManifest Compose(
        ClinicWorkspaceContext context,
        AgentExecutionContext? executionContext = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(options.Model))
            throw new InvalidOperationException("A configured clinic assistant model is required.");
        if (context.SubjectKind == "patient" && context.PatientId == Guid.Empty)
            throw new ArgumentException("A route-sourced patient id is required.", nameof(context));

        DateTimeOffset currentUtc = timeProvider.GetUtcNow();
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(context.TimeZone);
        AgentExecutionContext trustedExecution = executionContext ?? CreateExecutionContext(context, currentUtc, timeZone);
        IReadOnlyCollection<IClinicWorkspaceCapability> available = capabilityRegistry.GetAvailable(context);
        IReadOnlyCollection<IIntelligenceAgentTool> intelligence = intelligenceTools.ToArray();
        ClinicAssistantCompositionContext composition = new(context, available, intelligence, currentUtc);
        AgentProfile<ClinicAssistantCompositionContext> profile = context.SubjectKind == "practice"
            ? new ClinicOperationsAssistantProfile(composition, options)
            : new ClinicPatientAssistantProfile(composition, options);
        ComposedAgentManifest<ClinicAssistantCompositionContext> manifest = profileComposer.Compose(
            profile,
            composition,
            key => IsExposed(key, available, intelligence)
                ? new AgentExposureDecision(true, "enabled-v1")
                : AgentExposureDecision.Denied("denied-v1"));
        IReadOnlyCollection<IAgentTool> tools = toolDispatcher.CreateGuardedTools(
            context, trustedExecution, manifest.Tools, manifest.ToolCapabilities);
        ClinicAssistantExecutionCollector collector = new();
        Dictionary<string, IReadOnlySet<string>> actions = available.ToDictionary(
            capability => capability.Name,
            capability => (IReadOnlySet<string>)capability.ActionTypes.ToHashSet(StringComparer.Ordinal),
            StringComparer.Ordinal);
        return new(manifest.Definition, tools, actions, collector, trustedExecution, manifest.Limits, manifest.ToolCapabilities, manifest.CompatibilityKey);
    }

    private static bool IsExposed(
        string key,
        IReadOnlyCollection<IClinicWorkspaceCapability> available,
        IReadOnlyCollection<IIntelligenceAgentTool> intelligence) =>
        key == ClinicAssistantCapabilityKeys.Intelligence
            ? intelligence.Count > 0
            : available.Any(capability => string.Equals(capability.Name, key, StringComparison.Ordinal));

    private static AgentExecutionContext CreateExecutionContext(
        ClinicWorkspaceContext context,
        DateTimeOffset currentUtc,
        TimeZoneInfo timeZone)
    {
        DateTimeOffset currentLocal = TimeZoneInfo.ConvertTime(currentUtc, timeZone);
        Dictionary<string, string> metadata = new(StringComparer.Ordinal)
        {
            ["practiceId"] = context.PracticeId,
            ["patientId"] = context.PatientId.ToString("D"),
            ["authenticatedUserId"] = context.AuthenticatedUserId,
            ["permissions"] = JsonSerializer.Serialize(context.Permissions),
            ["role"] = context.Role,
            ["locale"] = context.Locale,
            ["calendar"] = context.Calendar,
            ["timeZone"] = context.TimeZone,
            ["currentUtc"] = currentUtc.ToString("O"),
            ["currentLocal"] = currentLocal.ToString("O"),
            ["conversationId"] = context.ConversationId,
            ["subjectKind"] = context.SubjectKind,
            ["agentKey"] = AgentKey(context)
        };
        if (!string.IsNullOrWhiteSpace(context.PatientNationalId)) metadata["patientNationalId"] = context.PatientNationalId;
        return new(Guid.NewGuid().ToString("N"), metadata);
    }

    private static string AgentKey(ClinicWorkspaceContext context) =>
        string.IsNullOrWhiteSpace(context.AgentKey)
            ? context.SubjectKind == "practice" ? "clinic.assistant.operations" : "clinic.assistant.patient"
            : context.AgentKey;

    private static string SubjectId(ClinicWorkspaceContext context) =>
        context.SubjectKind == "practice"
            ? IsLegacyAgentKey(context.AgentKey) ? "practice" : context.PracticeId
            : context.PatientId.ToString("D");

    private static bool IsLegacyAgentKey(string? agentKey) =>
        string.Equals(agentKey, "clinic.patient", StringComparison.Ordinal) ||
        string.Equals(agentKey, "clinic.operations", StringComparison.Ordinal);

    private async Task<IReadOnlyList<ClinicWorkspaceAction>> PersistActionsAsync(
        ClinicWorkspaceResult result,
        ClinicWorkspaceContext context,
        CancellationToken cancellationToken)
    {
        DateTimeOffset preparedAt = timeProvider.GetUtcNow();
        List<ClinicWorkspaceAction> actions = [];
        foreach (ClinicWorkspacePreparedAction prepared in result.PreparedActions)
        {
            ClinicWorkspaceAction action = new(
                prepared.OwningId,
                prepared.AgentRunId ?? result.AgentRunId,
                result.ConversationId,
                context.PatientId,
                context.PracticeId,
                context.AuthenticatedUserId,
                prepared.Capability,
                prepared.ActionType,
                prepared.OwningId,
                prepared.ReviewDataJson,
                ClinicWorkspaceActionStatus.Proposed,
                preparedAt,
                prepared.Expiration ?? preparedAt.AddMinutes(30),
                Summary: prepared.Summary,
                SubjectKind: context.SubjectKind,
                ToolCallId: prepared.ToolCallId,
                ToolName: prepared.ToolName);
            await actionStore.SaveAsync(action, cancellationToken);
            actions.Add(action);
        }

        return actions;
    }

    private static ClinicWorkspaceResult Parse(
        JsonElement output,
        ClinicAssistantManifest workspace,
        ClinicWorkspaceContext context,
        DateTimeOffset preparedAt,
        IReadOnlyList<AgentToolExecution> toolExecutions)
    {
        List<ClinicWorkspaceMessage> messages = [];
        foreach (JsonElement item in output.GetProperty("messages").EnumerateArray())
        {
            ClinicWorkspaceMessageKind kind = item.GetProperty("kind").GetString() switch
            {
                "assistantMessage" => ClinicWorkspaceMessageKind.AssistantMessage,
                "informationalToolResult" => ClinicWorkspaceMessageKind.InformationalToolResult,
                "missingInformationRequest" => ClinicWorkspaceMessageKind.MissingInformationRequest,
                "failure" => ClinicWorkspaceMessageKind.Failure,
                _ => throw new JsonException("Unknown clinic assistant message kind.")
            };
            messages.Add(new(kind, item.GetProperty("content").GetString() ?? string.Empty));
        }

        Dictionary<(string Capability, string ActionType), Queue<string>> summaries = new();
        foreach (JsonElement item in output.GetProperty("actions").EnumerateArray())
        {
            string capability = item.GetProperty("capability").GetString() ?? string.Empty;
            string actionType = item.GetProperty("actionType").GetString() ?? string.Empty;
            if (!workspace.EnabledActions.TryGetValue(capability, out IReadOnlySet<string>? actionTypes) || !actionTypes.Contains(actionType))
                throw new JsonException("The clinic assistant returned an action that is not enabled.");
            var key = (capability, actionType);
            if (!summaries.TryGetValue(key, out Queue<string>? values)) summaries[key] = values = new();
            values.Enqueue(item.GetProperty("summary").GetString() ?? string.Empty);
        }

        List<ClinicWorkspacePreparedAction> preparedActions = [];
        foreach (ClinicAssistantToolExecution execution in workspace.ExecutionCollector.Executions)
        {
            ClinicWorkspacePreparedAction prepared = execution.PreparedAction;
            if (!workspace.EnabledActions.TryGetValue(prepared.Capability, out IReadOnlySet<string>? registered) || !registered.Contains(prepared.ActionType))
                throw new JsonException("A preparation tool produced an action type that is not enabled.");
            summaries.TryGetValue((prepared.Capability, prepared.ActionType), out Queue<string>? values);
            preparedActions.Add(prepared with
            {
                Summary = values is { Count: > 0 } ? values.Dequeue() : string.Empty,
                ToolCallId = execution.ToolCallId,
                ToolName = execution.ToolName,
                AgentRunId = workspace.ExecutionContext.ExecutionId
            });
        }

        ClinicWorkspaceSearchResult? search = context.SubjectKind == "practice" ? ParseSearchResult(toolExecutions) : null;
        return new(workspace.ExecutionContext.ExecutionId, context.ConversationId, messages, [])
        {
            PreparedActions = preparedActions,
            Candidates = search?.Candidates,
            HasMoreCandidates = search?.HasMore ?? false
        };
    }

    private static ClinicWorkspaceSearchResult? ParseSearchResult(IReadOnlyList<AgentToolExecution> toolExecutions)
    {
        AgentToolExecution? execution = toolExecutions.LastOrDefault(item => !item.IsError && string.Equals(item.ToolName, "search_patients", StringComparison.Ordinal));
        if (execution is null) return null;
        try { return JsonSerializer.Deserialize(execution.Output, ClinicWorkspaceJsonSerializerContext.Default.ClinicWorkspaceSearchResult); }
        catch (JsonException) { return null; }
    }
}

internal sealed record ClinicAssistantManifest(
    AgentDefinition Definition,
    IReadOnlyCollection<IAgentTool> Tools,
    IReadOnlyDictionary<string, IReadOnlySet<string>> EnabledActions,
    ClinicAssistantExecutionCollector ExecutionCollector,
    AgentExecutionContext ExecutionContext,
    AgentExecutionLimits Limits,
    IReadOnlyDictionary<string, string> ToolCapabilities,
    string CompatibilityKey);

public static class ClinicAssistantCompositionServiceCollectionExtensions
{
    public static IServiceCollection AddClinicAssistantComposition(this IServiceCollection services)
    {
        _ = services.AddAgentComposition<ClinicAssistantCompositionContext>(
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.Appointments),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.AppointmentReads),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.Prescriptions),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.PrescriptionReads),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.PatientReminderReads),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.ClinicPatientReads),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.SeaPatientReads),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.ClinicalEvents),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.SeaManagement),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.Operations),
            new ClinicIntelligenceCapabilityPack());
        _ = services.AddTransient<ClinicAssistantRuntime>();
        _ = services.AddTransient<IClinicWorkspaceRuntime>(sp => sp.GetRequiredService<ClinicAssistantRuntime>());
        _ = services.AddTransient<IClinicWorkspaceGuardedToolResolver>(sp => sp.GetRequiredService<ClinicAssistantRuntime>());
        return services;
    }
}
