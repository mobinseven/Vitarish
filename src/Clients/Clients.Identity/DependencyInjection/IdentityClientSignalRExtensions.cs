using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Contracts;
using Vitarish.Clients.Identity.Internal;
using Vitarish.Clients.Identity.Options;

namespace Vitarish.Clients.Identity.DependencyInjection;

public static class IdentityClientSignalRExtensions
{
    public static IServiceCollection AddIdentitySignalRClient<TClient>(this IServiceCollection services)
        where TClient : ISignalRClientDefinition =>
        services.AddIdentitySignalRClient<TClient>(static _ => { });

    public static IServiceCollection AddIdentitySignalRClient<TClient>(this IServiceCollection services, Action<SignalRClientRegistration> configure)
        where TClient : ISignalRClientDefinition
    {
        ArgumentNullException.ThrowIfNull(configure);
        return services.AddIdentitySignalRClient(SignalRClientOptions.KeyFromType(typeof(TClient)), registration =>
        {
            TClient.Configure(registration);
            configure(registration);
        });
    }

    public static IServiceCollection AddIdentitySignalRClient(this IServiceCollection services, string name, Action<SignalRClientRegistration> configure)
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

        services.TryAddTransient<IIdentityClientSignalRFactory, IdentityClientSignalRFactory>();
        return services;
    }
}
