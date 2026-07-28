namespace Vitarish.Intelligence.Application.Agents;

public interface IAgentToolDispatcher
{
    Task<AgentToolResult> DispatchAsync(
        AgentToolCall call,
        IReadOnlyDictionary<string, IAgentTool> registeredTools,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default);
}
