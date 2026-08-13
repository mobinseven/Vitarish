using System.Text.Json;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class AssistantResultParser(AssistantCapabilityRegistry capabilityRegistry)
{
    internal AssistantResult Parse(
        JsonElement output,
        AssistantManifest manifest,
        AssistantContext context,
        IReadOnlyList<AgentToolExecution> toolExecutions)
    {
        if (output.ValueKind != JsonValueKind.Object ||
            !output.TryGetProperty("messages", out JsonElement messagesValue) ||
            !output.TryGetProperty("actions", out JsonElement actionsValue) ||
            messagesValue.ValueKind != JsonValueKind.Array || actionsValue.ValueKind != JsonValueKind.Array)
            throw new JsonException("The clinic assistant result shape is invalid.");

        List<AssistantMessage> messages = [];
        foreach (JsonElement item in messagesValue.EnumerateArray())
        {
            AssistantMessageKind kind = item.GetProperty("kind").GetString() switch
            {
                "assistantMessage" => AssistantMessageKind.AssistantMessage,
                "informationalToolResult" => AssistantMessageKind.InformationalToolResult,
                "missingInformationRequest" => AssistantMessageKind.MissingInformationRequest,
                "failure" => AssistantMessageKind.Failure,
                _ => throw new JsonException("Unknown clinic assistant message kind.")
            };
            messages.Add(new(kind, item.GetProperty("content").GetString() ?? string.Empty));
        }

        Dictionary<(string Capability, string ActionType), Queue<string>> summaries = new();
        foreach (JsonElement item in actionsValue.EnumerateArray())
        {
            string capability = item.GetProperty("capability").GetString() ?? string.Empty;
            string actionType = item.GetProperty("actionType").GetString() ?? string.Empty;
            AssistantActionPolicy? policy = capabilityRegistry.ResolveAction(actionType);
            if (!manifest.EnabledActions.TryGetValue(capability, out IReadOnlySet<string>? actionTypes) ||
                !actionTypes.Contains(actionType) || policy is null)
                throw new JsonException("The clinic assistant returned an action that is not enabled.");
            var key = (capability, actionType);
            if (!summaries.TryGetValue(key, out Queue<string>? values)) summaries[key] = values = new();
            values.Enqueue(item.GetProperty("summary").GetString() ?? string.Empty);
        }

        List<AssistantPreparedAction> preparedActions = [];
        foreach (AssistantToolExecution execution in manifest.ExecutionCollector.Executions)
        {
            AssistantPreparedAction prepared = execution.PreparedAction;
            AssistantActionPolicy? policy = capabilityRegistry.ResolveAction(prepared.ActionType);
            if (policy is null || !policy.RequiresConfirmation ||
                !manifest.EnabledActions.TryGetValue(prepared.Capability, out IReadOnlySet<string>? registered) ||
                !registered.Contains(prepared.ActionType))
                throw new JsonException("A preparation tool produced an action type that is not enabled.");
            summaries.TryGetValue((prepared.Capability, prepared.ActionType), out Queue<string>? values);
            preparedActions.Add(prepared with
            {
                Summary = values is { Count: > 0 } ? values.Dequeue() : string.Empty,
                ToolCallId = execution.ToolCallId,
                ToolName = execution.ToolName,
                AgentRunId = manifest.ExecutionContext.ExecutionId
            });
        }

        AssistantSearchResult? search = context.SubjectKind == "practice" ? ParseSearchResult(toolExecutions) : null;
        return new(manifest.ExecutionContext.ExecutionId, context.ConversationId, messages, [])
        {
            PreparedActions = preparedActions,
            Candidates = search?.Candidates,
            HasMoreCandidates = search?.HasMore ?? false
        };
    }

    private static AssistantSearchResult? ParseSearchResult(IReadOnlyList<AgentToolExecution> toolExecutions)
    {
        AgentToolExecution? execution = toolExecutions.LastOrDefault(item => !item.IsError && string.Equals(item.ToolName, "search_patients", StringComparison.Ordinal));
        if (execution is null) return null;
        try { return JsonSerializer.Deserialize(execution.Output, AssistantJsonSerializerContext.Default.AssistantSearchResult); }
        catch (JsonException) { return null; }
    }
}
