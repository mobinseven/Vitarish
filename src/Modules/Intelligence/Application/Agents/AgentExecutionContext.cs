namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentExecutionContext(
    string ExecutionId,
    IReadOnlyDictionary<string, string>? Metadata = null)
{
    public AgentRuntimeExecutionEnvelope? Envelope { get; init; }

    /// <summary>1-based ordinal of the runtime attempt of the turn; a history retry is a later attempt. Set by the conversation workflow.</summary>
    public int Attempt { get; init; } = 1;
}
