namespace Vitarish.Modules.Base.Platform.Retry;

/// <summary>
/// Defines retry policy for a specific provider. Used by the outbox
/// infrastructure to determine backoff and escalation behavior.
/// </summary>
public sealed record RetryPolicy
{
    /// <summary>Provider key this policy applies to.</summary>
    public required string ProviderKey { get; init; }

    /// <summary>Maximum number of retry attempts before escalation.</summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>Initial delay between retries.</summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Backoff multiplier for exponential backoff.</summary>
    public double BackoffMultiplier { get; init; } = 2.0;

    /// <summary>Maximum delay between retries.</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>Whether to fetch remote status before retry (duplicate-safe pattern).</summary>
    public bool FetchBeforeRetry { get; init; } = true;

    /// <summary>Timeout for individual retry attempts.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}
