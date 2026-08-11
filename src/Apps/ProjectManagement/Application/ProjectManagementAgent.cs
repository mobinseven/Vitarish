using MediatR;
using System.Text.Json;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Sea.Application.Agents;

namespace Vitarish.ProjectManagement.Application;

public sealed record ProjectManagementAgentContext(string TenantId, string UserId, Guid? EventId, string Locale, Guid? ConversationId = null, string SubjectKind = "event", string? SubjectId = null)
{
    public string EffectiveSubjectId => SubjectId ?? EventId?.ToString("D") ?? TenantId;
}

public sealed record RunProjectManagementAgent(ProjectManagementAgentContext Context, string Message) : IRequest<ProjectManagementAgentResult>;

public sealed record ProjectManagementAgentResult(AgentConversationOutcome Outcome, string? AgentRunId = null, Guid? ConversationId = null, System.Text.Json.JsonElement? Result = null);

public sealed class RunProjectManagementAgentHandler(
    ISeaManagementAgentTool seaTool,
    ISender sender,
    ProjectManagementAgentOptions options,
    IProjectManagementActionStore actions,
    TimeProvider timeProvider,
    ProjectManagementPresentationBuilder presentationBuilder)
    : IRequestHandler<RunProjectManagementAgent, ProjectManagementAgentResult>
{
    public async Task<ProjectManagementAgentResult> Handle(RunProjectManagementAgent request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return new(AgentConversationOutcome.InvalidHistory);
        }

        if (request.Context.EventId is not { } eventId || eventId == Guid.Empty ||
            !string.Equals(request.Context.SubjectKind, "event", StringComparison.Ordinal) ||
            !string.Equals(request.Context.EffectiveSubjectId, eventId.ToString("D"), StringComparison.Ordinal))
        {
            return new(AgentConversationOutcome.InvalidHistory);
        }

        const string agentKey = "project-management.event";
        AgentConversationScope scope = new(request.Context.TenantId, request.Context.UserId, agentKey, "event", eventId.ToString("D"));
        AgentExecutionContext execution = new(Guid.NewGuid().ToString("N"), new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["tenantId"] = request.Context.TenantId,
            ["authenticatedUserId"] = request.Context.UserId,
            ["locale"] = request.Context.Locale,
            ["subjectKind"] = "event",
            ["subjectId"] = eventId.ToString("D"),
            ["agentKey"] = agentKey
        });
        AgentDefinition definition = new(options.Model,
            "You are the assistant for one current event. Respond in the user's locale. Use sea_manage only for this event. Mutations are proposals and must never be claimed as complete before explicit confirmation.",
            new AgentResultDefinition("project_management_result", "{\"type\":\"object\",\"properties\":{\"parts\":{\"type\":\"array\",\"items\":{\"oneOf\":[{\"type\":\"object\",\"properties\":{\"type\":{\"const\":\"text\"},\"text\":{\"type\":\"string\"}},\"required\":[\"type\",\"text\"],\"additionalProperties\":false},{\"type\":\"object\",\"properties\":{\"type\":{\"const\":\"sea-reference\"},\"operation\":{\"type\":\"string\"},\"scopeId\":{\"type\":\"string\"},\"itemId\":{\"type\":\"string\"}},\"required\":[\"type\",\"operation\"],\"additionalProperties\":false}] }},\"actions\":{\"type\":\"array\"}},\"required\":[\"parts\",\"actions\"],\"additionalProperties\":false}"));
        List<ProjectManagementPreparedActionSummary> preparedActions = [];
        ProjectManagementPresentationResult? presentation = null;
        IReadOnlyCollection<IAgentTool> tools = [new EventScopedSeaTool(seaTool)];
        RunAgentConversation command = new(request.Context.ConversationId, scope, definition, new AgentInput(request.Message.Trim()), tools, execution, new AgentExecutionLimits(MaxToolCalls: 12, MaxIterations: 16, Timeout: TimeSpan.FromMinutes(2)), Presentation: (_, run) =>
        {
            if (presentation is null)
            {
                throw new InvalidOperationException("The presentation was not validated before persistence.");
            }

            return new AgentConversationPresentation(request.Message.Trim(), presentation.AssistantText, preparedActions.Select(x => x.ActionId).ToArray(), Parts: presentation.PersistedParts);
        }, BeforeCommit: async (_, run, callbackCancellationToken) =>
        {
            try { presentation = presentationBuilder.Build(run.StructuredOutput, run.ToolExecutions, request.Context, timeProvider.GetUtcNow()); }
            catch (JsonException) { return AgentConversationOutcome.InvalidHistory; }
            catch (UnauthorizedAccessException) { return AgentConversationOutcome.InvalidHistory; }
            catch (InvalidOperationException) { return AgentConversationOutcome.HistoryLimitExceeded; }
            foreach (AgentToolExecution toolExecution in run.ToolExecutions)
            {
                if (toolExecution.IsError || !string.Equals(toolExecution.ToolName, seaTool.Definition.Name, StringComparison.Ordinal))
                {
                    continue;
                }

                SeaManagementPreparedAction? prepared = seaTool.ParsePreparedAction(new AgentToolResult(toolExecution.ToolCallId, toolExecution.Output, toolExecution.IsError));
                if (prepared is null)
                {
                    continue;
                }

                await actions.SaveAsync(new ProjectManagementAction(prepared.ActionId, request.Context.TenantId, request.Context.UserId, eventId, execution.ExecutionId, toolExecution.ToolCallId, prepared, ProjectManagementActionStatus.Proposed, timeProvider.GetUtcNow(), prepared.ExpiresAt, ScopeKind: "event", SubjectId: eventId.ToString("D")), callbackCancellationToken);
                preparedActions.Add(new(prepared.ActionId, prepared.Operation, prepared.ExpiresAt));
            }
            return AgentConversationOutcome.Succeeded;
        });
        RunAgentConversationResult result = await sender.Send(command, cancellationToken);
        return result.Outcome == AgentConversationOutcome.Succeeded && result.Run?.StructuredOutput is { } output
            ? new(result.Outcome, execution.ExecutionId, result.ConversationId, System.Text.Json.JsonSerializer.SerializeToElement(new { assistant = output, preparedActions }))
            : new(result.Outcome, execution.ExecutionId, result.ConversationId);
    }

}

public sealed record ProjectManagementAgentOptions(string Model);
public sealed record ProjectManagementPreparedActionSummary(Guid ActionId, string Operation, DateTimeOffset ExpiresAt);

/// <summary>Limits the model-visible SEA schema; SEA independently enforces the same event anchor at execution.</summary>
internal sealed class EventScopedSeaTool(ISeaManagementAgentTool inner) : IAgentTool
{
    public AgentToolDefinition Definition { get; } = new(
        "sea_manage",
        "Read or prepare changes for the current event and its attachments only. Writes require confirmation.",
        """{"type":"object","properties":{"operation":{"type":"string","enum":["get_event","list_reminders","get_reminder","create_reminder","update_reminder","delete_reminder","list_attachments","count_attachments","update_event","delete_event","commit_event","uncommit_event","create_attachment","update_attachment","delete_attachment","mark_read","mark_unread"]},"payload":{"type":"object"}},"required":["operation"],"additionalProperties":false}""");

    public Task<AgentToolResult> ExecuteAsync(AgentToolCall call, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
        inner.ExecuteAsync(call, context, cancellationToken);
}
