
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Commands.UsageConfigurations;
using Vitarish.Sea.Application.Queries.UsageConfigurations;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterScoped]
internal class UsageConfigurationService(IServiceScopeFactory serviceScopeFactory, UsageConfigurationMapper mapper) : EntityService<UsageConfiguration, UsageConfigurationDescriptor, UsageConfigurationDto, UsageConfigurationDto, UsageConfigurationDescriptorDto>(serviceScopeFactory, mapper.ToDto, mapper.ToDomain, _ => new UsageConfigurationDescriptor()), IUsageConfigurationService
{
    public async Task<UsageConfigurationDto?> GetUsageConfigurationAsync(CancellationToken cancellationToken)
    {
        return (await GetAsync(new GetUsageConfigurationQuery(), cancellationToken)).GetValueOrDefault();
    }

    public new async Task<UsageConfigurationDto?> UpdateAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken)
    {
        return (await GetAsync(
            new UpdateUsageConfigurationCommand(
                usageConfigurationDto.ReminderSignature ?? string.Empty,
                usageConfigurationDto.Metadata),
            cancellationToken)).GetValueOrDefault();
    }
}

