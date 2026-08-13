namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentExecutionContext(
    string ExecutionId,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    public AgentRuntimeExecutionEnvelope? Envelope { get; init; }
}
