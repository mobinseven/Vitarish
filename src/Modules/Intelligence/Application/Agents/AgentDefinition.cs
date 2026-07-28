namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentDefinition(
    string Model,
    string Instructions,
    AgentResultDefinition? Result = null);

public sealed record AgentResultDefinition(
    string Name,
    string JsonSchema,
    bool Strict = true);
