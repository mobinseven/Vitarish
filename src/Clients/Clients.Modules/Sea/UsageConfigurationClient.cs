using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions
{
    public interface IUsageConfigurationClient
    {
        Task<UsageConfigurationDto?> GetAsync(CancellationToken cancellationToken = default);
        Task<UsageConfigurationDto?> UpdateAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken = default);
    }
}

namespace Vitarish.Clients.Modules.Sea.Internal.Services
{
    internal sealed class UsageConfigurationClient(IIdentityClientSignalRFactory connectionFactory)
        : SeaHubClient<UsageConfigurationClientDefinition>(connectionFactory),
            IUsageConfigurationClient
    {
        public async Task<UsageConfigurationDto?> GetAsync(CancellationToken cancellationToken = default) =>
            await InvokeAsync<UsageConfigurationDto?>(nameof(GetAsync), cancellationToken, Array.Empty<object?>()).ConfigureAwait(false);

        public async Task<UsageConfigurationDto?> UpdateAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken = default) =>
            await InvokeAsync<UsageConfigurationDto?>(nameof(UpdateAsync), cancellationToken, new object?[] { usageConfigurationDto }).ConfigureAwait(false);
    }
}
