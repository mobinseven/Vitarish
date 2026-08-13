using System.Text.Json;

namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentRuntimeRunRequest(
    AgentDefinition Definition,
    AgentInput Input,
    IReadOnlyCollection<AgentToolDefinition> Tools,
    AgentRuntimeExecutionEnvelope ExecutionEnvelope,
    AgentExecutionLimits Limits,
    string CallbackUrl,
    string CallbackCredential);

public sealed record AgentRuntimeRunResponse(
    bool IsSuccess,
    string? Output,
    JsonElement? StructuredOutput,
    AgentRunUsage Usage,
    int Iterations,
    int ToolCalls,
    IReadOnlyList<AgentToolExecution> ToolExecutions,
    string? History = null,
    AgentRunError? Error = null);

public interface IAgentRuntimeCredentialService
{
    string Create(AgentRuntimeExecutionEnvelope envelope);
    bool Validate(string credential, AgentRuntimeExecutionEnvelope envelope);
    bool ValidateCallback(string credential, AgentRuntimeExecutionEnvelope envelope);
}
