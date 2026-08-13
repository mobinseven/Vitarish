using Vitarish.Clinic.Application.ClinicWorkspace;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

internal sealed record ClinicAssistantToolExecution(
    string ToolName,
    string ToolCallId,
    string AgentRunId,
    ClinicWorkspacePreparedAction PreparedAction);

internal sealed class ClinicAssistantExecutionCollector
{
    private readonly List<ClinicAssistantToolExecution> _executions = [];
    private readonly object _sync = new();

    public IReadOnlyList<ClinicAssistantToolExecution> Executions
    {
        get { lock (_sync) return [.. _executions]; }
    }

    public void RecordRemote(
        IReadOnlyCollection<AgentToolExecution> executions,
        IReadOnlyCollection<IAgentTool> tools,
        IReadOnlyDictionary<string, string> toolCapabilities,
        AgentExecutionContext context)
    {
        Dictionary<string, IClinicWorkspacePreparationTool> preparationTools = tools
            .OfType<IClinicWorkspacePreparationTool>()
            .ToDictionary(tool => tool.Definition.Name, StringComparer.Ordinal);

        foreach (AgentToolExecution execution in executions)
        {
            if (execution.IsError ||
                !preparationTools.TryGetValue(execution.ToolName, out IClinicWorkspacePreparationTool? tool))
                continue;

            ClinicWorkspacePreparedAction? prepared = tool.GetPreparedAction(
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
