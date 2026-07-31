namespace Vitarish.Intelligence.Domain.Options;

public sealed class AgentConversationRetentionOptions
{
    public int RetentionDays { get; init; } = 90;
    public int CleanupIntervalHours { get; init; } = 24;
}
