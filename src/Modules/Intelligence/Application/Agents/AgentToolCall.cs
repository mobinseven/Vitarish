namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentToolCall(
    string Id,
    string Name,
    string ArgumentsJson);
