using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Clients.Gateway.Abstractions;
using Vitarish.Clients.Gateway.Contracts;
using Vitarish.Clients.Gateway.Internal;
using Vitarish.Clients.Gateway.Options;

namespace Vitarish.Clients.Gateway.DependencyInjection;

public static class SignalRClientServiceCollectionExtensions
{
    public static IServiceCollection AddSignalRClient<TClient>(this IServiceCollection services)
        where TClient : ISignalRClientDefinition =>
        services.AddSignalRClient<TClient>(static _ => { });

    public static IServiceCollection AddSignalRClient<TClient>(this IServiceCollection services, Action<SignalRClientRegistration> configure)
        where TClient : ISignalRClientDefinition
    {
        ArgumentNullException.ThrowIfNull(configure);
        return services.AddSignalRClient(SignalRClientOptions.KeyFromType(typeof(TClient)), registration =>
        {
            TClient.Configure(registration);
            configure(registration);
        });
    }

    public static IServiceCollection AddSignalRClient(this IServiceCollection services, string name, Action<SignalRClientRegistration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        _ = services.Configure<SignalRClientOptions>(options =>
        {
            SignalRClientRegistration registration = new();
            configure(registration);
            registration.Validate();
            options.Clients[name] = registration;
        });

        services.TryAddTransient<ISignalRClientFactory, SignalRClientFactory>();
        return services;
    }
}
