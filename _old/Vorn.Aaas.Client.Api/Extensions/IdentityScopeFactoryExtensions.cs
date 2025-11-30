using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Contracts;
public static class IdentityScopeFactoryExtensions
{
    public static async Task RunInScopeAsync(
        this IIdentityScopeFactory factory,
        ScopeRequestIdentity identity,
        Func<IServiceProvider, Task> action)
    {
        // assume CreateScopeAsync returns Task<AsyncServiceScope>
        await using AsyncServiceScope scope = await factory.CreateScopeAsync(identity);

        var serviceProvider = scope.ServiceProvider;

        // now resolve something and run your action:
        await action(serviceProvider);

        // when this method exits, the scope will be disposed

    }
}
