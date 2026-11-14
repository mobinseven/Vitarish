namespace Vitarish.Intelligence.Application.Agents;

/// <param name="SurfaceCatalog">
/// For a tool whose argument is a presented surface (ADR-0022 decision 8): the catalog document the agent runtime
/// validates the surface against before calling back. Null for every other tool.
/// </param>
public sealed record AgentToolDefinition(
    string Name,
    string Description,
    string ParametersJsonSchema,
    string? SurfaceCatalog = null);
