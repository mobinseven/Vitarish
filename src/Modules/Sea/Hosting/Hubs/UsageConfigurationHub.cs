using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class UsageConfigurationHub(IUsageConfigurationService service, IEntityUserService? entityUserService = null)
    : EntityServer<UsageConfigurationDto, UsageConfigurationWriteDto, UsageConfigurationDescriptorDto, IUsageConfigurationService>(service, entityUserService)
{
    public Task<UsageConfigurationDto?> GetUsageConfigurationAsync() =>
        EntityService.GetUsageConfigurationAsync(CancellationToken.None);

    public Task<UsageConfigurationDto?> UpdateUsageConfigurationAsync(UsageConfigurationWriteDto usageConfigurationDto) =>
        EntityService.UpdateAsync(usageConfigurationDto, CancellationToken.None);
}
