using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Clinic.Application.ClinicWorkspace.Actions;
using Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class ClinicAssistantConversationRunner(
    ClinicAssistantManifestComposer manifestComposer,
    ClinicAssistantResultParser resultParser,
    ISender sender,
    IClinicWorkspaceActionStore actionStore,
    ClinicWorkspaceCapabilityRegistry capabilityRegistry,
    TimeProvider timeProvider,
    ILogger<ClinicAssistantConversationRunner>? logger = null)
{
    public async Task<ClinicWorkspaceResult> RunAsync(
        ClinicWorkspaceContext context,
        ClinicWorkspaceInput input,
        CancellationToken cancellationToken = default)
    {
        ClinicAssistantManifest manifest = manifestComposer.Compose(context);
        if (string.IsNullOrWhiteSpace(input.Message))
            return ClinicWorkspaceResult.Failure(manifest.ExecutionContext.ExecutionId, context.ConversationId, "A message is required.");

        Guid? existingConversationId = context.IsNewConversation
            ? null
            : Guid.TryParse(context.ConversationId, out Guid parsedConversationId)
                ? parsedConversationId
                : Guid.Empty;
        ClinicWorkspaceResult? parsedResult = null;
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
                    ClinicWorkspaceContext persistedContext = context with
                    {
                        ConversationId = conversationId.ToString("N"),
                        IsNewConversation = false
                    };
                    parsedResult = resultParser.Parse(output, manifest, persistedContext, run.ToolExecutions);
                    if (parsedResult.PreparedActions.Count > 0)
                    {
                        IReadOnlyList<ClinicWorkspaceAction> actions = await PersistActionsAsync(
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
            ClinicAssistantTelemetry.ProviderCompleted(
                logger,
                runResult.Error?.Code.ToString() ?? "failed",
                manifest.ExecutionContext.ExecutionId);
        if (logger is not null)
            ClinicAssistantTelemetry.ConversationCompleted(
                logger,
                conversation.Outcome.ToString(),
                manifest.ExecutionContext.ExecutionId,
                conversation.ConversationId?.ToString("N") ?? context.ConversationId);
        if (conversation.Outcome != AgentConversationOutcome.Succeeded ||
            runResult is null || !runResult.IsSuccess || runResult.StructuredOutput is not JsonElement)
        {
            if (logger is not null && conversation.Outcome == AgentConversationOutcome.ProfileChanged)
                ClinicAssistantTelemetry.ProfileConflict(
                    logger,
                    manifest.ExecutionContext.ExecutionId,
                    conversation.ConversationId?.ToString("N") ?? context.ConversationId);
            ClinicWorkspaceResult failure = ClinicWorkspaceResult.Failure(
                manifest.ExecutionContext.ExecutionId,
                conversation.ConversationId?.ToString("N") ?? context.ConversationId,
                runResult?.Error?.Message ?? "The clinic assistant did not return a valid result.");
            return failure with
            {
                Outcome = conversation.Outcome switch
                {
                    AgentConversationOutcome.NotFound => ClinicWorkspaceRunOutcome.NotFound,
                    AgentConversationOutcome.Busy => ClinicWorkspaceRunOutcome.Busy,
                    AgentConversationOutcome.ProfileChanged => ClinicWorkspaceRunOutcome.ProfileChanged,
                    AgentConversationOutcome.InvalidHistory or AgentConversationOutcome.HistoryLimitExceeded => ClinicWorkspaceRunOutcome.InvalidHistory,
                    _ => ClinicWorkspaceRunOutcome.Failed
                }
            };
        }

        return parsedResult is null
            ? ClinicWorkspaceResult.Failure(manifest.ExecutionContext.ExecutionId, context.ConversationId, "The clinic assistant returned an invalid structured result.")
            : parsedResult with { ConversationId = conversation.ConversationId!.Value.ToString("N") };
    }

    private async Task<IReadOnlyList<ClinicWorkspaceAction>> PersistActionsAsync(
        ClinicWorkspaceResult result,
        ClinicWorkspaceContext context,
        CancellationToken cancellationToken)
    {
        DateTimeOffset preparedAt = timeProvider.GetUtcNow();
        List<ClinicWorkspaceAction> actions = [];
        foreach (ClinicWorkspacePreparedAction prepared in result.PreparedActions)
        {
            ClinicWorkspaceActionPolicy? policy = capabilityRegistry.ResolveAction(prepared.ActionType);
            if (policy is null || !policy.RequiresConfirmation || prepared.ToolName is null ||
                capabilityRegistry.ResolveTool(prepared.ToolName) is not { } toolPolicy ||
                !string.Equals(toolPolicy.ActionType, policy.ActionType, StringComparison.Ordinal))
                throw new InvalidOperationException("The prepared action does not have a valid Clinic action policy.");

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
                prepared.Expiration ?? preparedAt.Add(policy.DefaultProposalLifetime ?? TimeSpan.FromMinutes(30)),
                Summary: prepared.Summary,
                SubjectKind: context.SubjectKind,
                ToolCallId: prepared.ToolCallId,
                ToolName: prepared.ToolName,
                ActionPolicyRevision: policy.Revision);
            await actionStore.SaveAsync(action, cancellationToken);
            if (logger is not null)
                ClinicAssistantTelemetry.PreparedActionCreated(logger, action.ActionType, action.ActionId, action.AgentRunId);
            actions.Add(action);
        }
        return actions;
    }

    private static string AgentKey(ClinicWorkspaceContext context) =>
        ClinicAssistantExecutionContextFactory.AgentKey(context);

    private static string SubjectId(ClinicWorkspaceContext context) =>
        ClinicAssistantExecutionContextFactory.SubjectId(context);

}
