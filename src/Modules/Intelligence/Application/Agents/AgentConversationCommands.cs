using MediatR;

namespace Vitarish.Intelligence.Application.Agents;

public sealed record RunAgentConversation(
    Guid? ConversationId,
    AgentConversationScope Scope,
    AgentDefinition Definition,
    AgentInput Input,
    IReadOnlyCollection<IAgentTool> Tools,
    AgentExecutionContext ExecutionContext,
    AgentExecutionLimits Limits)
    : IRequest<RunAgentConversationResult>;

public sealed record RunAgentConversationResult(
    AgentConversationOutcome Outcome,
    Guid? ConversationId = null,
    AgentRunResult? Run = null);

public sealed record DeleteAgentConversation(
    Guid ConversationId,
    AgentConversationScope Scope)
    : IRequest<AgentConversationOutcome>;

public sealed class RunAgentConversationHandler(
    IAgentConversationStore store,
    IAgentRunner runner)
    : IRequestHandler<RunAgentConversation, RunAgentConversationResult>
{
    public const string HistoryFormat = "openai-agents-js-history/v1";
    public static readonly TimeSpan LeaseSafetyMargin = TimeSpan.FromSeconds(30);

    public async Task<RunAgentConversationResult> Handle(
        RunAgentConversation request,
        CancellationToken cancellationToken)
    {
        TimeSpan runtimeTimeout = request.Limits.EffectiveTimeout;
        AgentConversationStoreResult acquired = await store.AcquireAsync(
            request.ConversationId,
            request.Scope,
            runtimeTimeout + LeaseSafetyMargin,
            cancellationToken);
        if (acquired.Outcome != AgentConversationOutcome.Succeeded || acquired.Lease is null)
            return new(acquired.Outcome);

        AgentConversationLease lease = acquired.Lease;
        try
        {
            AgentRunResult run = await runner.RunAsync(
                request.Definition,
                request.Input with { History = lease.History },
                request.Tools,
                request.ExecutionContext,
                request.Limits,
                cancellationToken);

            if (!run.IsSuccess)
                return new(
                    run.Error?.Code == AgentRunErrorCode.InvalidProviderResponse
                        ? AgentConversationOutcome.InvalidHistory
                        : AgentConversationOutcome.ProviderFailure,
                    lease.ConversationId,
                    run);
            if (string.IsNullOrWhiteSpace(run.History))
                return new(AgentConversationOutcome.InvalidHistory, lease.ConversationId, run);

            AgentConversationOutcome replaced = await store.ReplaceHistoryAsync(
                lease,
                request.Scope,
                run.History,
                HistoryFormat,
                cancellationToken);
            return new(replaced, lease.ConversationId, run);
        }
        finally
        {
            await store.ReleaseAsync(lease, CancellationToken.None);
        }
    }
}

public sealed class DeleteAgentConversationHandler(IAgentConversationStore store)
    : IRequestHandler<DeleteAgentConversation, AgentConversationOutcome>
{
    public Task<AgentConversationOutcome> Handle(
        DeleteAgentConversation request,
        CancellationToken cancellationToken) =>
        store.DeleteAsync(request.ConversationId, request.Scope, cancellationToken);
}
