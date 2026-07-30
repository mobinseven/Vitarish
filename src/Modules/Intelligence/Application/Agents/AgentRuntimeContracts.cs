using System.Text.Json;

namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentRuntimeRunRequest(
    AgentDefinition Definition,
    AgentInput Input,
    IReadOnlyCollection<AgentToolDefinition> Tools,
    AgentExecutionContext Context,
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
    string? ResponseId = null,
    AgentRunError? Error = null);

public interface IAgentRuntimeCredentialService
{
    string Create(AgentExecutionContext context, TimeSpan lifetime);
    bool Validate(string credential, AgentExecutionContext context);
}
