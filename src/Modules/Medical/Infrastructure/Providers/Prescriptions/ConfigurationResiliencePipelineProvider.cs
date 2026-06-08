using Microsoft.Extensions.Configuration;
using Vitarish.Medical.Application.Abstractions;

namespace Vitarish.Medical.Infrastructure.Providers.Prescriptions;

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
        IConfigurationSection section = configuration.GetSection("Prescription:Providers");
        if (!section.Exists())
        {
            return;
        }

        foreach (IConfigurationSection providerSection in section.GetChildren())
        {
            string key = providerSection.Key;
            IConfigurationSection resilienceSection = providerSection.GetSection("Resilience");
            if (!resilienceSection.Exists())
            {
                continue;
            }

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

    public ProviderResilienceOptions GetOptions(string providerKey) => _options.GetValueOrDefault(providerKey, _default);
}
