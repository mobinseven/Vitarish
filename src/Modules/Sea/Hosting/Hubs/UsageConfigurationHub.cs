using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class UsageConfigurationHub(IUsageConfigurationService service, IEntityUserService? entityUserService = null)
    : EntityServer<UsageConfigurationDto, UsageConfigurationWriteDto, UsageConfigurationDescriptorDto, IUsageConfigurationService>(service, entityUserService)
{
    public Task<UsageConfigurationDto?> GetUsageConfigurationAsync(CancellationToken cancellationToken = default) =>
        EntityService.GetUsageConfigurationAsync(cancellationToken);

    public Task<UsageConfigurationDto?> UpdateUsageConfigurationAsync(UsageConfigurationWriteDto usageConfigurationDto, CancellationToken cancellationToken = default) =>
        EntityService.UpdateAsync(usageConfigurationDto, cancellationToken);
}
