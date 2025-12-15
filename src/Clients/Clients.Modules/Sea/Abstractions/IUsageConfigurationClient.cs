using Vitarish.Clients.Modules.Sea.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IUsageConfigurationClient
{
    Task<UsageConfigurationDto?> GetUsageConfigurationAsync(CancellationToken cancellationToken = default);
    Task<UsageConfigurationDto?> UpdateUsageConfigurationAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken = default);
}
