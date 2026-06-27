using Microsoft.Extensions.DependencyInjection;

namespace Vitarish.Modules.Base.Client.DependencyInjection;

public static class InternalModuleClientRegistrationExtensions
{
    public static IHttpClientBuilder AddInternalModuleApiClient<TClientContract, TClientImplementation>(this IServiceCollection services)
        where TClientContract : class
        where TClientImplementation : class, TClientContract =>
        services.AddHttpClient<TClientContract, TClientImplementation>();

    public static IHttpClientBuilder AddInternalModuleApiClient<TClientContract, TClientImplementation, TClientAlias>(this IServiceCollection services)
        where TClientContract : class, TClientAlias
        where TClientImplementation : class, TClientContract
        where TClientAlias : class
    {
        IHttpClientBuilder builder = services.AddInternalModuleApiClient<TClientContract, TClientImplementation>();
        _ = services.AddTransient<TClientAlias>(sp => sp.GetRequiredService<TClientContract>());
        return builder;
    }

    public static IHttpClientBuilder AddInternalModuleApiClient<TClientContract, TClientImplementation, TFirstClientAlias, TSecondClientAlias>(this IServiceCollection services)
        where TClientContract : class, TFirstClientAlias, TSecondClientAlias
        where TClientImplementation : class, TClientContract
        where TFirstClientAlias : class
        where TSecondClientAlias : class
    {
        IHttpClientBuilder builder = services.AddInternalModuleApiClient<TClientContract, TClientImplementation, TFirstClientAlias>();
        _ = services.AddTransient<TSecondClientAlias>(sp => sp.GetRequiredService<TClientContract>());
        return builder;
    }
}
