using System.Text.Json;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;
using Vitarish.Clinic.Application.ClinicWorkspace.Operations;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class ClinicAssistantResultParser(ClinicWorkspaceCapabilityRegistry capabilityRegistry)
{
    internal ClinicWorkspaceResult Parse(
        JsonElement output,
        ClinicAssistantManifest manifest,
        ClinicWorkspaceContext context,
        IReadOnlyList<AgentToolExecution> toolExecutions)
    {
        if (output.ValueKind != JsonValueKind.Object ||
            !output.TryGetProperty("messages", out JsonElement messagesValue) ||
            !output.TryGetProperty("actions", out JsonElement actionsValue) ||
            messagesValue.ValueKind != JsonValueKind.Array || actionsValue.ValueKind != JsonValueKind.Array)
            throw new JsonException("The clinic assistant result shape is invalid.");

        List<ClinicWorkspaceMessage> messages = [];
        foreach (JsonElement item in messagesValue.EnumerateArray())
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
        foreach (JsonElement item in actionsValue.EnumerateArray())
        {
            string capability = item.GetProperty("capability").GetString() ?? string.Empty;
            string actionType = item.GetProperty("actionType").GetString() ?? string.Empty;
            ClinicWorkspaceActionPolicy? policy = capabilityRegistry.ResolveAction(actionType);
            if (!manifest.EnabledActions.TryGetValue(capability, out IReadOnlySet<string>? actionTypes) ||
                !actionTypes.Contains(actionType) || policy is null)
                throw new JsonException("The clinic assistant returned an action that is not enabled.");
            var key = (capability, actionType);
            if (!summaries.TryGetValue(key, out Queue<string>? values)) summaries[key] = values = new();
            values.Enqueue(item.GetProperty("summary").GetString() ?? string.Empty);
        }

        List<ClinicWorkspacePreparedAction> preparedActions = [];
        foreach (ClinicAssistantToolExecution execution in manifest.ExecutionCollector.Executions)
        {
            ClinicWorkspacePreparedAction prepared = execution.PreparedAction;
            ClinicWorkspaceActionPolicy? policy = capabilityRegistry.ResolveAction(prepared.ActionType);
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

        ClinicWorkspaceSearchResult? search = context.SubjectKind == "practice" ? ParseSearchResult(toolExecutions) : null;
        return new(manifest.ExecutionContext.ExecutionId, context.ConversationId, messages, [])
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
