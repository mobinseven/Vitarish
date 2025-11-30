using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.Client.Api.Contracts;

namespace Vorn.Aaas.Client.Api.Abstractions;

/// <summary>
/// Factory for creating service scopes pre-initialized with an <see cref="IRequestIdentityContext"/>.
/// Useful for non-HTTP scenarios (e.g., background jobs, message handlers) where identity context
/// needs to be established outside of a request pipeline.
/// </summary>
public interface IIdentityScopeFactory
{
    /// <summary>
    /// Creates a new service scope with an initialized <see cref="IRequestIdentityContext"/>.
    /// </summary>
    /// <param name="identity">The identity information to initialize the context with.</param>
    /// <returns>An async service scope that supports async disposal.</returns>
    Task<AsyncServiceScope> CreateScopeAsync(ScopeRequestIdentity identity);
}
