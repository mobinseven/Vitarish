using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Modules.Base.DependencyInjection;

public static class InternalModuleClientRegistrationExtensions
{
    public static IHttpClientBuilder AddInternalModuleApiClient<TClientContract, TClientImplementation>(this IServiceCollection services)
        where TClientContract : class
        where TClientImplementation : class, TClientContract =>
        services.AddHttpClient<TClientContract, TClientImplementation>();
}
