using System.Text.Json;
using MediatR;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Clinic.Application.PatientAgent.Actions;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed class RunPatientAgentHandler(
    IAgentRunner agentRunner,
    PatientWorkspaceAgentDefinitionFactory definitionFactory,
    IPatientAgentActionStore actionStore,
    TimeProvider timeProvider)
    : IRequestHandler<RunPatientAgent, PatientAgentResult>
{
    public async Task<PatientAgentResult> Handle(RunPatientAgent request, CancellationToken cancellationToken)
    {
        PatientWorkspaceAgentDefinition workspace = definitionFactory.Create(request.Context);
        if (string.IsNullOrWhiteSpace(request.Input.Message))
        {
            return PatientAgentResult.Failure(workspace.ExecutionContext.ExecutionId, request.Context.ConversationId, request.Context.PreviousResponseId, "A message is required.");
        }

        AgentRunResult run = await agentRunner.RunAsync(
            workspace.Definition,
            new AgentInput(request.Input.Message, request.Context.PreviousResponseId),
            workspace.Tools,
            workspace.ExecutionContext,
            workspace.Limits,
            cancellationToken);

        if (!run.IsSuccess || run.StructuredOutput is not JsonElement output)
        {
            return PatientAgentResult.Failure(workspace.ExecutionContext.ExecutionId, request.Context.ConversationId, request.Context.PreviousResponseId, run.Error?.Message ?? "The patient agent did not return a valid result.");
        }

        try
        {
            if (run.ToolExecutions.Count > 0)
            {
                workspace.ExecutionCollector.RecordRemote(
                    run.ToolExecutions,
                    workspace.Tools,
                    workspace.ToolCapabilities ?? new Dictionary<string, string>(StringComparer.Ordinal),
                    workspace.ExecutionContext);
            }
            PatientAgentResult result = Parse(output, workspace, request.Context, timeProvider.GetUtcNow(), run.ResponseId);
            foreach (PatientAgentAction action in result.Actions)
            {
                await actionStore.SaveAsync(action, cancellationToken);
            }
            return result;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return PatientAgentResult.Failure(workspace.ExecutionContext.ExecutionId, request.Context.ConversationId, request.Context.PreviousResponseId, "The patient agent returned an invalid structured result.");
        }
    }

    private static PatientAgentResult Parse(
        JsonElement output,
        PatientWorkspaceAgentDefinition workspace,
        PatientAgentContext context,
        DateTimeOffset preparedAt,
        string? responseId)
    {
        List<PatientAgentMessage> messages = [];
        foreach (JsonElement item in output.GetProperty("messages").EnumerateArray())
        {
            PatientAgentMessageKind kind = item.GetProperty("kind").GetString() switch
            {
                "assistantMessage" => PatientAgentMessageKind.AssistantMessage,
                "informationalToolResult" => PatientAgentMessageKind.InformationalToolResult,
                "missingInformationRequest" => PatientAgentMessageKind.MissingInformationRequest,
                "failure" => PatientAgentMessageKind.Failure,
                _ => throw new JsonException("Unknown patient agent message kind.")
            };
            messages.Add(new PatientAgentMessage(kind, item.GetProperty("content").GetString() ?? string.Empty));
        }

        Dictionary<(string Capability, string ActionType), Queue<string>> summaries = new();
        foreach (JsonElement item in output.GetProperty("actions").EnumerateArray())
        {
            string capability = item.GetProperty("capability").GetString() ?? string.Empty;
            string actionType = item.GetProperty("actionType").GetString() ?? string.Empty;
            if (!workspace.EnabledActions.TryGetValue(capability, out IReadOnlySet<string>? actionTypes) || !actionTypes.Contains(actionType))
            {
                throw new JsonException("The patient agent returned an action that is not enabled.");
            }

            var key = (capability, actionType);
            if (!summaries.TryGetValue(key, out Queue<string>? values)) summaries[key] = values = new();
            values.Enqueue(item.GetProperty("summary").GetString() ?? string.Empty);
        }

        List<PatientAgentAction> actions = [];
        foreach (PatientAgentToolExecution execution in workspace.ExecutionCollector.Executions)
        {
            PatientAgentPreparedAction prepared = execution.PreparedAction;
            if (!workspace.EnabledActions.TryGetValue(prepared.Capability, out IReadOnlySet<string>? registered) ||
                !registered.Contains(prepared.ActionType))
                throw new JsonException("A preparation tool produced an action type that is not enabled.");
            summaries.TryGetValue((prepared.Capability, prepared.ActionType), out Queue<string>? values);
            actions.Add(new PatientAgentAction(
                prepared.OwningId, workspace.ExecutionContext.ExecutionId, context.ConversationId,
                context.PatientId, context.PracticeId, context.AuthenticatedUserId,
                prepared.Capability, prepared.ActionType, prepared.OwningId, prepared.ReviewDataJson,
                PatientAgentActionStatus.Proposed, preparedAt,
                prepared.Expiration ?? preparedAt.AddMinutes(30),
                Summary: values is { Count: > 0 } ? values.Dequeue() : string.Empty));
        }
        return new PatientAgentResult(workspace.ExecutionContext.ExecutionId, context.ConversationId, responseId, messages, actions);
    }
}
