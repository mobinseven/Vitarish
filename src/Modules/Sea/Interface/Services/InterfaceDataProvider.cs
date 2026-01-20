using Injectio.Attributes;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Interface.Models;

namespace Vitarish.Sea.Interface.Services;

[RegisterScoped]
internal class InterfaceDataProvider(IUsersService UsersService, IUsageConfigurationService UsageConfigurationService, IRequestContext RequestContext) : IInterfaceDataProvider
{
    public IReadOnlyList<UserDto>? Users { get; private set; }
    public UsageConfigurationDto? UsageConfiguration { get; private set; }
    public UsageConfigurationMetadata? UsageConfigurationMetadata => UsageConfiguration?.MetadataObject as UsageConfigurationMetadata;

    public async Task Initialize()
    {
        UsageConfiguration ??= await UsageConfigurationService.GetUsageConfigurationAsync(CancellationToken.None);
        _ = (UsageConfiguration?.MetadataObject ??= UsageConfigurationMetadata.FromJson(UsageConfiguration?.Metadata));
        Users ??= [.. (await UsersService.GetUsersInRoleAsync(RequestContext.GetTenantUserRole(), CancellationToken.None)).Select(u => new UserDto(u))];
    }
}

public interface IInterfaceDataProvider
{
    IReadOnlyList<UserDto>? Users { get; }
    UsageConfigurationDto? UsageConfiguration { get; }
    UsageConfigurationMetadata? UsageConfigurationMetadata { get; }

    Task Initialize();
}
