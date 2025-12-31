using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Application.Abstractions;

public interface IUsageConfigurationService : IEntityService<UsageConfigurationDto, UsageConfigurationWriteDto, UsageConfigurationDescriptorDto>
{
    Task<UsageConfigurationDto?> GetUsageConfigurationAsync(CancellationToken cancellationToken);
    Task<UsageConfigurationDto?> UpdateAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken);
}

