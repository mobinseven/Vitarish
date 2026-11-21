using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Application.GuidedSurfaces;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;

namespace Vitarish.Clinic.Agents;

public sealed class AssistantConversationRunner(
    AssistantManifestComposer manifestComposer,
    AssistantResultParser resultParser,
    ISender sender,
    PreparedActionLifecycle preparedActions,
    AssistantCapabilityRegistry capabilityRegistry,
    TimeProvider timeProvider,
    PresentedSurfaceCollector surfaces,
    ILogger<AssistantConversationRunner>? logger = null)
{
    public async Task<AssistantResult> RunAsync(
        AssistantContext context,
        AssistantInput input,
        CancellationToken cancellationToken = default,
        IAgentTurnObserver? observer = null)
    {
        AssistantManifest manifest = manifestComposer.Compose(context);
        if (string.IsNullOrWhiteSpace(input.Message))
            return AssistantResult.Failure(manifest.ExecutionContext.ExecutionId, context.ConversationId, "A message is required.");

        // Chosen in advance from the entry the request used, never from user text. The entry has no tools, so
        // its answer field is the only text a streamed turn may show before the validated result.
        bool help = AssistantSubjects.IsOperationsHelp(context);
        if (help && (manifest.Tools.Count > 0 || manifest.EnabledActions.Count > 0))
            return AssistantResult.Failure(manifest.ExecutionContext.ExecutionId, context.ConversationId, "The clinic operations help entry must not expose tools or actions.");
        AgentAnswerStream? answerStream = help && observer is not null
            ? new AgentAnswerStream(manifest.ExecutionContext.ExecutionId, [AssistantRuntime.OperationsHelpAnswerField])
            : null;

        Guid? existingConversationId = context.IsNewConversation
            ? null
            : Guid.TryParse(context.ConversationId, out Guid parsedConversationId)
                ? parsedConversationId
                : Guid.Empty;
        AssistantResult? parsedResult = null;
        RunAgentConversationResult conversation = await sender.Send(new RunAgentConversation(
            existingConversationId,
            AssistantSubjects.ConversationScope(context),
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
                    if (help)
                    {
                        parsedResult = AssistantResultParser.ParseOperationsHelp(output, manifest, persistedContext, run.ToolExecutions);
                        return AgentConversationOutcome.Succeeded;
                    }
                    parsedResult = resultParser.Parse(output, manifest, persistedContext, run.ToolExecutions);
                    parsedResult = parsedResult with
                    {
                        Surfaces = await surfaces.CollectAsync(manifest.ExecutionContext.ExecutionId, run.ToolExecutions, callbackCancellationToken)
                    };
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
            ProfileCompatibilityKey: manifest.CompatibilityKey,
            Observer: observer,
            AnswerStream: answerStream),
            cancellationToken);

        AgentRunResult? runResult = conversation.Run;
        if (logger is not null && runResult is not null && !runResult.IsSuccess)
            AssistantTelemetry.ProviderCompleted(
                logger,
                runResult.Error?.Code.ToString() ?? AgentTurnOutcomes.Failed,
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
                    AgentConversationOutcome.InvalidHistory or AgentConversationOutcome.HistoryLimitExceeded => AssistantRunOutcome.InvalidHistory,
                    AgentConversationOutcome.TimedOut => AssistantRunOutcome.TimedOut,
                    AgentConversationOutcome.Cancelled => AssistantRunOutcome.Cancelled,
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

            PreparedAction action = ClinicPreparedActionMapping.ToPrepared(
                context,
                prepared,
                prepared.AgentRunId ?? result.AgentRunId,
                result.ConversationId,
                preparedAt,
                prepared.Expiration ?? preparedAt.Add(policy.DefaultProposalLifetime ?? TimeSpan.FromMinutes(30)),
                policy.Revision);
            await preparedActions.PrepareAsync(action, cancellationToken);
            actions.Add(ClinicPreparedActionMapping.ToClinic(action));
        }
        return actions;
    }
}
