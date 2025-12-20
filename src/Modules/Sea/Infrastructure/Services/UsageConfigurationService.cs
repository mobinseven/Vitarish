
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Commands.UsageConfigurations;
using Vitarish.Sea.Application.Queries.UsageConfigurations;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterScoped]
internal class UsageConfigurationService(IMediator mediator, UsageConfigurationMapper mapper) : EntityService<UsageConfiguration, UsageConfigurationDescriptor, UsageConfigurationDto, UsageConfigurationWriteDto, UsageConfigurationDescriptorDto>(mediator, mapper.ToDto, mapper.ToDomain, _ => new UsageConfigurationDescriptor()), IUsageConfigurationService
{
    public async Task<UsageConfigurationDto?> GetUsageConfigurationAsync(CancellationToken cancellationToken)
    {
        return (await GetAsync(new GetUsageConfigurationQuery(), cancellationToken)).GetValueOrDefault();
    }

    public async Task<UsageConfigurationDto?> UpdateAsync(UsageConfigurationWriteDto usageConfigurationDto, CancellationToken cancellationToken)
    {
        return (await GetAsync(
            new UpdateUsageConfigurationCommand(
                usageConfigurationDto.ReminderSignature ?? string.Empty,
                usageConfigurationDto.Metadata),
            cancellationToken)).GetValueOrDefault();
    }
}

