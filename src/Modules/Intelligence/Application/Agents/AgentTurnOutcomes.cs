namespace Vitarish.Intelligence.Application.Agents;

/// <summary>
/// The one owner of the outcome names shared by the conversation request, workflow and attempt
/// telemetry. Outcome names are log dimensions only and carry no prompt, answer or correlation value.
/// </summary>
public static class AgentTurnOutcomes
{
    public const string Succeeded = "Succeeded";
    public const string Cancelled = "Cancelled";
    public const string Faulted = "Faulted";
    public const string Failed = "Failed";
}
