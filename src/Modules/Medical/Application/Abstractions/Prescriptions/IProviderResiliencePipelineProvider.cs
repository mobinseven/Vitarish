namespace Vitarish.Medical.Application.Abstractions.Prescriptions;

/// <summary>
/// Provides per-provider resilience configuration for adapter operations.
/// Resilience includes timeout, retry, and circuit-breaker behavior.
/// </summary>
public interface IProviderResiliencePipelineProvider
{
    /// <summary>Gets the resilience options for a specific provider.</summary>
    ProviderResilienceOptions GetOptions(string providerKey);
}

/// <summary>
/// Configuration for per-provider resilience behavior.
/// </summary>
public sealed record ProviderResilienceOptions
{
    /// <summary>Timeout per individual request attempt.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Maximum number of retry attempts.</summary>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>Initial delay between retries (exponential backoff applied).</summary>
    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>Maximum delay between retries.</summary>
    public TimeSpan RetryMaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Number of consecutive failures before the circuit opens.</summary>
    public int CircuitBreakerFailureThreshold { get; init; } = 5;

    /// <summary>Duration the circuit stays open before allowing a test request.</summary>
    public TimeSpan CircuitBreakerDuration { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>Sampling duration for circuit breaker failure ratio calculation.</summary>
    public TimeSpan CircuitBreakerSamplingDuration { get; init; } = TimeSpan.FromMinutes(2);

    /// <summary>Default options for providers without specific configuration.</summary>
    public static ProviderResilienceOptions Default => new();
}
