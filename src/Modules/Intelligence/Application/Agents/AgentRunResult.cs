using System.Text.Json;

namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentRunResult(
    bool IsSuccess,
    string? Output,
    JsonElement? StructuredOutput,
    AgentRunUsage Usage,
    TimeSpan ProviderLatency,
    int Iterations,
    int ToolCalls,
    AgentRunError? Error = null)
{
    public string? History { get; init; }
    public IReadOnlyList<AgentToolExecution> ToolExecutions { get; init; } = [];
}

public sealed record AgentToolExecution(
    string ToolName,
    string ToolCallId,
    string Output,
    bool IsError = false,
    string? ArgumentsJson = null);

public sealed record AgentRunUsage(int InputTokens, int OutputTokens)
{
    public int TotalTokens => InputTokens + OutputTokens;
}

public sealed record AgentRunError(AgentRunErrorCode Code, string Message);

public enum AgentRunErrorCode
{
    InvalidRequest,
    UnknownTool,
    MalformedToolArguments,
    ToolFailure,
    IterationLimitExceeded,
    ToolCallLimitExceeded,
    TokenLimitExceeded,
    Timeout,
    Cancelled,
    ProviderUnavailable,
    ProviderFailure,
    InvalidProviderResponse,
    InvalidStructuredResult
}
