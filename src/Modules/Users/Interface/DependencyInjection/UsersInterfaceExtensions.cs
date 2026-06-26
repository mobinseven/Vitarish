using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Base.DependencyInjection;
using Vitarish.Users.Client;

namespace Vitarish.Users.Interface.DependencyInjection;

public static class UsersInterfaceExtensions
{
    public static IServiceCollection AddUsersInterface(this IServiceCollection services)
    {
        _ = services.AddInternalModuleApiClient<IUsersClient, UsersHttpClient>();
        return services;
    }
}
