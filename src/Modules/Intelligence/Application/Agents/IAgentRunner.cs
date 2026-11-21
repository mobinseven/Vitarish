namespace Vitarish.Intelligence.Application.Agents;

public interface IAgentRunner
{
    Task<AgentRunResult> RunAsync(
        AgentDefinition definition,
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentExecutionLimits limits,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs one attempt and reports the runtime's trusted stages to <paramref name="progress"/>. A runner
    /// without a streamed transport runs the attempt unstreamed and reports no stages.
    /// </summary>
    Task<AgentRunResult> RunStreamedAsync(
        AgentDefinition definition,
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentExecutionLimits limits,
        IAgentRunProgress progress,
        CancellationToken cancellationToken = default) =>
        RunAsync(definition, input, tools, context, limits, cancellationToken);
}
