using Vorn.Entities;
using Vorn.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class UsageConfigurationHub : EntityServer<UsageConfigurationDto, UsageConfigurationDescriptorDto, IUsageConfigurationService>
{
    private readonly IUsageConfigurationService _service;

    public UsageConfigurationHub(IUsageConfigurationService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<UsageConfigurationDto?> GetUsageConfigurationAsync(CancellationToken cancellationToken) =>
        _service.GetUsageConfigurationAsync(cancellationToken);

    public Task<UsageConfigurationDto?> UpdateUsageConfigurationAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken) =>
        _service.UpdateAsync(usageConfigurationDto, cancellationToken);
}
