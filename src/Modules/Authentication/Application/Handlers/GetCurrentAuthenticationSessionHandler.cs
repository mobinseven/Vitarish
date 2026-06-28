using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Application.Results;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class GetCurrentAuthenticationSessionHandler(IAuthenticationUserDirectory users)
{
    public async Task<AuthenticationResult<AuthenticationUserDescriptor?>> Handle(GetCurrentAuthenticationSessionQuery query, CancellationToken cancellationToken) =>
        new AuthenticationResult<AuthenticationUserDescriptor?>.Success(await users.GetCurrentAsync(cancellationToken));
}
