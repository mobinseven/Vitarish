namespace Vitarish.Intelligence.Application.Agents;

public sealed class RegisteredAgentToolDispatcher : IAgentToolDispatcher
{
    public Task<AgentToolResult> DispatchAsync(
        AgentToolCall call,
        IReadOnlyDictionary<string, IAgentTool> registeredTools,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (!registeredTools.TryGetValue(call.Name, out IAgentTool? tool) ||
            !string.Equals(tool.Definition.Name, call.Name, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The requested agent tool is not registered.");
        }

        return tool.ExecuteAsync(call, context, cancellationToken);
    }
}
