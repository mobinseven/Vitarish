using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed record PatientAgentPreparedAction(
    string Capability,
    string ActionType,
    Guid OwningId,
    string ReviewDataJson,
    DateTimeOffset? Expiration);

public sealed record PatientAgentToolExecution(
    string ToolName,
    string ToolCallId,
    string AgentRunId,
    PatientAgentPreparedAction PreparedAction);

public interface IPatientAgentPreparationTool : IAgentTool
{
    PatientAgentPreparedAction? GetPreparedAction(AgentToolResult result);
}

public sealed class PatientAgentExecutionCollector
{
    private readonly List<PatientAgentToolExecution> _executions = [];
    private readonly object _sync = new();

    public IReadOnlyList<PatientAgentToolExecution> Executions
    {
        get { lock (_sync) return [.. _executions]; }
    }

    public void Record(
        string capability,
        IAgentTool tool,
        AgentToolCall call,
        AgentExecutionContext context,
        AgentToolResult result)
    {
        if (result.IsError || tool is not IPatientAgentPreparationTool preparationTool) return;
        PatientAgentPreparedAction? prepared = preparationTool.GetPreparedAction(result);
        if (prepared is null) return;

        if (!string.Equals(prepared.Capability, capability, StringComparison.Ordinal))
            throw new InvalidOperationException("The prepared action capability does not match its registered capability.");

        lock (_sync)
        {
            _executions.Add(new(call.Name, call.Id, context.ExecutionId, prepared));
        }
    }

    public void RecordRemote(
        IReadOnlyCollection<AgentToolExecution> executions,
        IReadOnlyCollection<IAgentTool> tools,
        IReadOnlyDictionary<string, string> toolCapabilities,
        AgentExecutionContext context)
    {
        Dictionary<string, IPatientAgentPreparationTool> preparationTools = tools
            .OfType<IPatientAgentPreparationTool>()
            .ToDictionary(tool => tool.Definition.Name, StringComparer.Ordinal);

        foreach (AgentToolExecution execution in executions)
        {
            if (execution.IsError ||
                !preparationTools.TryGetValue(execution.ToolName, out IPatientAgentPreparationTool? tool))
                continue;

            PatientAgentPreparedAction? prepared = tool.GetPreparedAction(
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
