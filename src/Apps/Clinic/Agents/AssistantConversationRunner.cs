using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class AssistantConversationRunner(
    AssistantManifestComposer manifestComposer,
    AssistantResultParser resultParser,
    ISender sender,
    IAssistantActionStore actionStore,
    AssistantCapabilityRegistry capabilityRegistry,
    TimeProvider timeProvider,
    ILogger<AssistantConversationRunner>? logger = null)
{
    public async Task<AssistantResult> RunAsync(
        AssistantContext context,
        AssistantInput input,
        CancellationToken cancellationToken = default)
    {
        AssistantManifest manifest = manifestComposer.Compose(context);
        if (string.IsNullOrWhiteSpace(input.Message))
            return AssistantResult.Failure(manifest.ExecutionContext.ExecutionId, context.ConversationId, "A message is required.");

        Guid? existingConversationId = context.IsNewConversation
            ? null
            : Guid.TryParse(context.ConversationId, out Guid parsedConversationId)
                ? parsedConversationId
                : Guid.Empty;
        AssistantResult? parsedResult = null;
        RunAgentConversationResult conversation = await sender.Send(new RunAgentConversation(
            existingConversationId,
            new AgentConversationScope(
                context.PracticeId,
                context.AuthenticatedUserId,
                AgentKey(context),
                context.SubjectKind,
                SubjectId(context)),
            manifest.Definition,
            new AgentInput(input.Message),
            manifest.Tools,
            manifest.ExecutionContext,
            manifest.Limits,
            async (conversationId, run, callbackCancellationToken) =>
            {
                try
                {
                    if (run.StructuredOutput is not JsonElement output)
                        return AgentConversationOutcome.InvalidHistory;
                    manifest.ExecutionCollector.RecordRemote(
                        run.ToolExecutions,
                        manifest.Tools,
                        manifest.ToolCapabilities,
                        manifest.ExecutionContext);
                    AssistantContext persistedContext = context with
                    {
                        ConversationId = conversationId.ToString("N"),
                        IsNewConversation = false
                    };
                    parsedResult = resultParser.Parse(output, manifest, persistedContext, run.ToolExecutions);
                    if (parsedResult.PreparedActions.Count > 0)
                    {
                        IReadOnlyList<AssistantAction> actions = await PersistActionsAsync(
                            parsedResult,
                            persistedContext,
                            callbackCancellationToken);
                        parsedResult = parsedResult with { Actions = actions, PreparedActions = [] };
                    }
                    return AgentConversationOutcome.Succeeded;
                }
                catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
                {
                    return AgentConversationOutcome.InvalidHistory;
                }
            },
            ProfileCompatibilityKey: manifest.CompatibilityKey),
            cancellationToken);

        AgentRunResult? runResult = conversation.Run;
        if (logger is not null && runResult is not null && !runResult.IsSuccess)
            AssistantTelemetry.ProviderCompleted(
                logger,
                runResult.Error?.Code.ToString() ?? "failed",
                manifest.ExecutionContext.ExecutionId);
        if (logger is not null)
            AssistantTelemetry.ConversationCompleted(
                logger,
                conversation.Outcome.ToString(),
                manifest.ExecutionContext.ExecutionId,
                conversation.ConversationId?.ToString("N") ?? context.ConversationId);
        if (conversation.Outcome != AgentConversationOutcome.Succeeded ||
            runResult is null || !runResult.IsSuccess || runResult.StructuredOutput is not JsonElement)
        {
            if (logger is not null && conversation.Outcome == AgentConversationOutcome.ProfileChanged)
                AssistantTelemetry.ProfileConflict(
                    logger,
                    manifest.ExecutionContext.ExecutionId,
                    conversation.ConversationId?.ToString("N") ?? context.ConversationId);
            AssistantResult failure = AssistantResult.Failure(
                manifest.ExecutionContext.ExecutionId,
                conversation.ConversationId?.ToString("N") ?? context.ConversationId,
                runResult?.Error?.Message ?? "The clinic assistant did not return a valid result.");
            return failure with
            {
                Outcome = conversation.Outcome switch
                {
                    AgentConversationOutcome.NotFound => AssistantRunOutcome.NotFound,
                    AgentConversationOutcome.Busy => AssistantRunOutcome.Busy,
                    AgentConversationOutcome.ProfileChanged => AssistantRunOutcome.ProfileChanged,
                    AgentConversationOutcome.InvalidHistory or AgentConversationOutcome.HistoryLimitExceeded => AssistantRunOutcome.InvalidHistory,
                    _ => AssistantRunOutcome.Failed
                }
            };
        }

        return parsedResult is null
            ? AssistantResult.Failure(manifest.ExecutionContext.ExecutionId, context.ConversationId, "The clinic assistant returned an invalid structured result.")
            : parsedResult with { ConversationId = conversation.ConversationId!.Value.ToString("N") };
    }

    private async Task<IReadOnlyList<AssistantAction>> PersistActionsAsync(
        AssistantResult result,
        AssistantContext context,
        CancellationToken cancellationToken)
    {
        DateTimeOffset preparedAt = timeProvider.GetUtcNow();
        List<AssistantAction> actions = [];
        foreach (AssistantPreparedAction prepared in result.PreparedActions)
        {
            AssistantActionPolicy? policy = capabilityRegistry.ResolveAction(prepared.ActionType);
            if (policy is null || !policy.RequiresConfirmation || prepared.ToolName is null ||
                capabilityRegistry.ResolveTool(prepared.ToolName) is not { } toolPolicy ||
                !string.Equals(toolPolicy.ActionType, policy.ActionType, StringComparison.Ordinal))
                throw new InvalidOperationException("The prepared action does not have a valid Clinic action policy.");

            AssistantAction action = new(
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
                AssistantActionStatus.Proposed,
                preparedAt,
                prepared.Expiration ?? preparedAt.Add(policy.DefaultProposalLifetime ?? TimeSpan.FromMinutes(30)),
                Summary: prepared.Summary,
                SubjectKind: context.SubjectKind,
                ToolCallId: prepared.ToolCallId,
                ToolName: prepared.ToolName,
                ActionPolicyRevision: policy.Revision);
            await actionStore.SaveAsync(action, cancellationToken);
            if (logger is not null)
                AssistantTelemetry.PreparedActionCreated(logger, action.ActionType, action.ActionId, action.AgentRunId);
            actions.Add(action);
        }
        return actions;
    }

    private static string AgentKey(AssistantContext context) =>
        AssistantExecutionContextFactory.AgentKey(context);

    private static string SubjectId(AssistantContext context) =>
        AssistantExecutionContextFactory.SubjectId(context);

}
