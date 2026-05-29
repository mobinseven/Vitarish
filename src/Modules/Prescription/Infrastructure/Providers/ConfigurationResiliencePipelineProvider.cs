using Microsoft.Extensions.Configuration;
using Vitarish.Prescription.Application.Abstractions;

namespace Vitarish.Prescription.Infrastructure.Providers;

/// <summary>
/// Configuration-driven resilience options provider.
/// Reads per-provider settings from configuration, falling back to defaults.
/// </summary>
public sealed class ConfigurationResiliencePipelineProvider : IProviderResiliencePipelineProvider
{
    private readonly Dictionary<string, ProviderResilienceOptions> _options = new(StringComparer.OrdinalIgnoreCase);
    private readonly ProviderResilienceOptions _default = ProviderResilienceOptions.Default;

    public ConfigurationResiliencePipelineProvider(IConfiguration configuration)
    {
        var section = configuration.GetSection("Prescription:Providers");
        if (!section.Exists())
            return;

        foreach (var providerSection in section.GetChildren())
        {
            var key = providerSection.Key;
            var resilienceSection = providerSection.GetSection("Resilience");
            if (!resilienceSection.Exists())
                continue;

            _options[key] = new ProviderResilienceOptions
            {
                Timeout = TimeSpan.FromSeconds(resilienceSection.GetValue("TimeoutSeconds", 30)),
                MaxRetryAttempts = resilienceSection.GetValue("MaxRetryAttempts", 3),
                RetryBaseDelay = TimeSpan.FromSeconds(resilienceSection.GetValue("RetryBaseDelaySeconds", 2)),
                RetryMaxDelay = TimeSpan.FromSeconds(resilienceSection.GetValue("RetryMaxDelaySeconds", 30)),
                CircuitBreakerFailureThreshold = resilienceSection.GetValue("CircuitBreakerFailureThreshold", 5),
                CircuitBreakerDuration = TimeSpan.FromSeconds(resilienceSection.GetValue("CircuitBreakerDurationSeconds", 60)),
                CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(resilienceSection.GetValue("CircuitBreakerSamplingDurationSeconds", 120)),
            };
        }
    }

    public ProviderResilienceOptions GetOptions(string providerKey)
    {
        return _options.GetValueOrDefault(providerKey, _default);
    }
}
