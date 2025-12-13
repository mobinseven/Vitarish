namespace Vitarish.Sea.Application.Abstractions;

public interface IUsageConfigurationRepository
{
    Task<UsageConfiguration?> GetAsync(CancellationToken cancellationToken = default);
    Task<UsageConfiguration?> UpdateAsync(UsageConfiguration entity, CancellationToken cancellationToken = default);
}

