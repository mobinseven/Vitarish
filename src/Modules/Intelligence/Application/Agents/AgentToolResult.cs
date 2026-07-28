namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentToolResult(
    string CallId,
    string Output,
    bool IsError = false);
