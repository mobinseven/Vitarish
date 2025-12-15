using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Application.Abstractions;

public interface IUsageConfigurationService : IEntityService<UsageConfigurationDto, UsageConfigurationDescriptorDto>
{
    Task<UsageConfigurationDto?> GetUsageConfigurationAsync(CancellationToken cancellationToken);
    new Task<UsageConfigurationDto?> UpdateAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken);
}

