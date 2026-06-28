using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Application.Results;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class GetLogoutContextHandler(ILogoutContextService contexts)
{
    public async Task<AuthenticationResult<LogoutContext?>> Handle(GetLogoutContextQuery query, CancellationToken cancellationToken) =>
        new AuthenticationResult<LogoutContext?>.Success(await contexts.GetAsync(query.LogoutId, cancellationToken));
}
