using Vitarish.Entities.Server.Core;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class UsageConfigurationHub : EntityServer<UsageConfigurationDto, UsageConfigurationDto, UsageConfigurationDescriptorDto, IUsageConfigurationService>
{
    private readonly IUsageConfigurationService _service;

    public UsageConfigurationHub(IServiceScopeFactory serviceScopeFactory, IUsageConfigurationService service, IEntityUserService? entityUserService = null)
        : base(serviceScopeFactory, entityUserService)
    {
        _service = service;
    }

    public Task<UsageConfigurationDto?> GetUsageConfigurationAsync(CancellationToken cancellationToken) =>
        _service.GetUsageConfigurationAsync(cancellationToken);

    public Task<UsageConfigurationDto?> UpdateUsageConfigurationAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken) =>
        _service.UpdateAsync(usageConfigurationDto, cancellationToken);
}
