using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure;

internal sealed class PersonalCredentialHost(IAuthenticationHostDirectory hosts) : IPersonalCredentialHost
{
    public Task<bool> AllowsManagementAsync(CancellationToken cancellationToken) =>
        hosts.SignInOrigin is null ? Task.FromResult(true) : hosts.IsCentralHostAsync(cancellationToken);
}
