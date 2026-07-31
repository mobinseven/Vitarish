using System.Text.Json;
using MediatR;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Clinic.Application.PatientAgent.Actions;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed class RunPatientAgentHandler(
    ISender sender,
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
            return PatientAgentResult.Failure(workspace.ExecutionContext.ExecutionId, request.Context.ConversationId, "A message is required.");
        }

        Guid? existingConversationId = request.Context.IsNewConversation
            ? null
            : Guid.TryParse(request.Context.ConversationId, out Guid parsedConversationId)
                ? parsedConversationId
                : Guid.Empty;
        PatientAgentResult? parsedResult = null;
        string agentKey = request.Context.SubjectKind == "practice" ? "clinic.operations" : "clinic.patient";
        string subjectId = request.Context.SubjectKind == "practice" ? request.Context.PracticeId : request.Context.PatientId.ToString("D");
        RunAgentConversationResult conversation = await sender.Send(new RunAgentConversation(
            existingConversationId,
            new AgentConversationScope(
                request.Context.PracticeId,
                request.Context.AuthenticatedUserId,
                agentKey,
                request.Context.SubjectKind,
                subjectId),
            workspace.Definition,
            new AgentInput(request.Input.Message),
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
                            workspace.ToolCapabilities ?? new Dictionary<string, string>(StringComparer.Ordinal),
                            workspace.ExecutionContext);
                    }
                    PatientAgentContext persistedContext = request.Context with
                    {
                        ConversationId = conversationId.ToString("N"),
                        IsNewConversation = false
                    };
                    parsedResult = Parse(output, workspace, persistedContext, timeProvider.GetUtcNow());
                    foreach (PatientAgentAction action in parsedResult.Actions)
                        await actionStore.SaveAsync(action, callbackCancellationToken);
                    return AgentConversationOutcome.Succeeded;
                }
                catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
                {
                    return AgentConversationOutcome.InvalidHistory;
                }
            }), cancellationToken);
        AgentRunResult? run = conversation.Run;

        if (conversation.Outcome != AgentConversationOutcome.Succeeded ||
            run is null ||
            !run.IsSuccess ||
            run.StructuredOutput is not JsonElement output)
        {
            PatientAgentResult failure = PatientAgentResult.Failure(
                workspace.ExecutionContext.ExecutionId,
                conversation.ConversationId?.ToString("N") ?? request.Context.ConversationId,
                run?.Error?.Message ?? "The patient agent did not return a valid result.");
            return failure with
            {
                Outcome = conversation.Outcome switch
                {
                    AgentConversationOutcome.NotFound => PatientAgentRunOutcome.NotFound,
                    AgentConversationOutcome.Busy => PatientAgentRunOutcome.Busy,
                    _ => PatientAgentRunOutcome.Failed
                }
            };
        }

        if (conversation.Outcome != AgentConversationOutcome.Succeeded || parsedResult is null)
            return PatientAgentResult.Failure(workspace.ExecutionContext.ExecutionId, request.Context.ConversationId, "The patient agent returned an invalid structured result.");
        return parsedResult with
        {
            ConversationId = conversation.ConversationId!.Value.ToString("N")
        };
    }

    private static PatientAgentResult Parse(
        JsonElement output,
        PatientWorkspaceAgentDefinition workspace,
        PatientAgentContext context,
        DateTimeOffset preparedAt)
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
                Summary: values is { Count: > 0 } ? values.Dequeue() : string.Empty,
                SubjectKind: context.SubjectKind,
                ToolCallId: execution.ToolCallId,
                ToolName: execution.ToolName));
        }
        return new PatientAgentResult(workspace.ExecutionContext.ExecutionId, context.ConversationId, messages, actions);
    }
}
