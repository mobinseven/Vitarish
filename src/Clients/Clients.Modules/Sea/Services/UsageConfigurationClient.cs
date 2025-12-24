using System;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Services
{
    internal sealed class UsageConfigurationClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<UsageConfigurationDto, UsageConfigurationDto, UsageConfigurationDescriptorDto, UsageConfigurationClientDefinition>(connectionFactory, entityInterceptionProvider),
            IUsageConfigurationClient
    {
        public async Task<UsageConfigurationDto?> GetUsageConfigurationAsync(CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<UsageConfigurationDto?>(nameof(GetUsageConfigurationAsync), cancellationToken, []).ConfigureAwait(false);

        public async Task<UsageConfigurationDto?> UpdateUsageConfigurationAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<UsageConfigurationDto?>(nameof(UpdateUsageConfigurationAsync), cancellationToken, [usageConfigurationDto]).ConfigureAwait(false);
    }
}
