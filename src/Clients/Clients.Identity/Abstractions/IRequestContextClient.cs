
using Vitarish.Clients.Identity.Contracts;

namespace Vitarish.Clients.Identity.Abstractions;

/// <summary>
/// Provides access to client identity context information sourced from options and claims.
/// This is the client-side alternative to the server-side IRequestIdentityContext.
/// </summary>
public interface IRequestContextClient
{
    RequestDto? Request { get; }

    Task Initialize();
}
