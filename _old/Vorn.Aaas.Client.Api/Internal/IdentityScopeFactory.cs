using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Contracts;

namespace Vorn.Aaas.Client.Api.Internal;

/// <summary>
/// Factory implementation that creates service scopes with pre-initialized identity context.
/// This factory is stateless and singleton-safe.
/// </summary>
internal sealed class IdentityScopeFactory : IIdentityScopeFactory
{
    private readonly IServiceScopeFactory _scopeFactory;

    public IdentityScopeFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Create a new async DI scope, and initialize the IRequestIdentityContext inside it.
    /// Caller is responsible for awaiting the returned scope’s disposal (await using).
    /// </summary>
    public async Task<AsyncServiceScope> CreateScopeAsync(ScopeRequestIdentity identity)
    {
        // Create the new scope
        AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();

        try
        {
            // Resolve the generator (which itself should be scoped or transient)
            var ctxGenerator = scope.ServiceProvider.GetRequiredService<IRequestIdentityContextGenerator>();

            // Run initialization
            await ctxGenerator.Generate(identity);

            // Return the scope for usage; caller will dispose it
            return scope;
        }
        catch
        {
            // If initialization fails, ensure we dispose the scope to avoid leaks
            await scope.DisposeAsync();
            throw;
        }
    }
}

