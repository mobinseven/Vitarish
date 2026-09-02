using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class GetAuthenticationAccessHistoryHandler(
    IAuthenticationSessionRegistry sessionRegistry,
    IRequestContext requestContext)
{
    public Task<AuthenticationAccessHistory?> Handle(GetAuthenticationAccessHistoryQuery query, CancellationToken cancellationToken) =>
        sessionRegistry.GetAccessHistoryAsync(requestContext.UserId ?? string.Empty, requestContext.TenantId, cancellationToken);
}
