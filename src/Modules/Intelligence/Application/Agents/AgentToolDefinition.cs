namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentToolDefinition(
    string Name,
    string Description,
    string ParametersJsonSchema);
