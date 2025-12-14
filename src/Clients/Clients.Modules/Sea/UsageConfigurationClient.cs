using System;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Internal
{
    internal sealed class UsageConfigurationClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<UsageConfigurationClientDefinition>(connectionFactory, entityInterceptionProvider),
            IUsageConfigurationClient
    {
        public async Task<UsageConfigurationDto?> GetAsync(CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<UsageConfigurationDto?>(nameof(GetAsync), cancellationToken, Array.Empty<object?>()).ConfigureAwait(false);

        public async Task<UsageConfigurationDto?> UpdateAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<UsageConfigurationDto?>(nameof(UpdateAsync), cancellationToken, new object?[] { usageConfigurationDto }).ConfigureAwait(false);
    }
}
