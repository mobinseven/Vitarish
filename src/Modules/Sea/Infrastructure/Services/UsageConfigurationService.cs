using Vitarish.Sea.Application.Commands.UsageConfigurations;
using Vitarish.Sea.Application.Queries.UsageConfigurations;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal class UsageConfigurationService(IMediator mediator, UsageConfigurationMapper mapper) : EntityService<UsageConfiguration, UsageConfigurationDescriptor, UsageConfigurationDto, UsageConfigurationWriteDto, UsageConfigurationDescriptorDto>(mediator, mapper.ToDto, mapper.ToDomain, _ => new UsageConfigurationDescriptor()), IUsageConfigurationService
{
    public async Task<UsageConfigurationDto?> GetUsageConfigurationAsync(CancellationToken cancellationToken) => (await GetAsync(new GetUsageConfigurationQuery(), cancellationToken)).GetValueOrDefault();

    public async Task<UsageConfigurationDto?> UpdateAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken) => (await GetAsync(
            new UpdateUsageConfigurationCommand(
                usageConfigurationDto.ReminderSignature ?? string.Empty,
                usageConfigurationDto.Metadata),
            cancellationToken)).GetValueOrDefault();
}

