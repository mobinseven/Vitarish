using Vitarish.Clinic.Application.Assistant;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

internal sealed record AssistantToolExecution(
    string ToolName,
    string ToolCallId,
    string AgentRunId,
    AssistantPreparedAction PreparedAction);

internal sealed class AssistantExecutionCollector
{
    private readonly List<AssistantToolExecution> _executions = [];
    private readonly object _sync = new();

    public IReadOnlyList<AssistantToolExecution> Executions
    {
        get { lock (_sync) return [.. _executions]; }
    }

    public void RecordRemote(
        IReadOnlyCollection<AgentToolExecution> executions,
        IReadOnlyCollection<IAgentTool> tools,
        IReadOnlyDictionary<string, string> toolCapabilities,
        AgentExecutionContext context)
    {
        Dictionary<string, IAssistantPreparationTool> preparationTools = tools
            .OfType<IAssistantPreparationTool>()
            .ToDictionary(tool => tool.Definition.Name, StringComparer.Ordinal);

        foreach (AgentToolExecution execution in executions)
        {
            if (execution.IsError ||
                !preparationTools.TryGetValue(execution.ToolName, out IAssistantPreparationTool? tool))
                continue;

            AssistantPreparedAction? prepared = tool.GetPreparedAction(
                new AgentToolResult(execution.ToolCallId, execution.Output, execution.IsError));
            if (prepared is null) continue;
            if (!toolCapabilities.TryGetValue(execution.ToolName, out string? capability) ||
                !string.Equals(prepared.Capability, capability, StringComparison.Ordinal))
                throw new InvalidOperationException("The prepared action capability does not match its registered capability.");

            lock (_sync)
            {
                _executions.Add(new(execution.ToolName, execution.ToolCallId, context.ExecutionId, prepared));
            }
        }
    }
}
