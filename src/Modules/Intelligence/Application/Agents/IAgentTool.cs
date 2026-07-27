namespace Vitarish.Intelligence.Application.Agents;

public interface IAgentTool
{
    AgentToolDefinition Definition { get; }

    Task<AgentToolResult> ExecuteAsync(
        AgentToolCall call,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default);
}
