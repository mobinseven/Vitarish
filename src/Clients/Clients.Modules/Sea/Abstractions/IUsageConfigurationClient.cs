using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IUsageConfigurationClient
{
    Task<UsageConfigurationDto?> GetAsync(CancellationToken cancellationToken = default);
    Task<UsageConfigurationDto?> UpdateAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken = default);
}
