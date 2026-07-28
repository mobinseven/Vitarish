namespace Vitarish.Intelligence.Application.Agents;

public sealed record AgentExecutionLimits(
    int MaxIterations = 8,
    int MaxToolCalls = 16,
    int MaxTokens = 8_192,
    TimeSpan? Timeout = null)
{
    public TimeSpan EffectiveTimeout => Timeout ?? TimeSpan.FromMinutes(2);
}
