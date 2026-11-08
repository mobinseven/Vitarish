using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.ReturnUrls;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class AuthenticationCreationHandoff(IAuthenticationHostDirectory hosts, IAuthenticationSessionRootReader roots,
    IRequestContext request, IssueTenantHandoffHandler handoffs) : IAuthenticationCreationHandoff
{
    public async Task<string?> CreateAsync(string userId, string clinicHostname, CancellationToken cancellationToken)
    {
        if (hosts.SignInOrigin is not { } auth || request.UserId != userId ||
            request.User?.FindFirst(AuthenticationSessionClaimTypes.SessionId)?.Value is not { } sessionId ||
            !Uri.TryCreate(hosts.CurrentOrigin, UriKind.Absolute, out Uri? source)) return null;
        Guid? root = await roots.FindAsync(sessionId, userId, cancellationToken);
        if (root is null) return null;
        string target = new UriBuilder(source) { Host = clinicHostname, Path = "/", Query = string.Empty, Fragment = string.Empty }.Uri.GetLeftPart(UriPartial.Authority);
        if (!await hosts.CanOpenAsync(target, userId, cancellationToken)) return null;
        string? token = await handoffs.Handle(new IssueTenantHandoffCommand(userId, new Uri(auth).Host,
            PriorCentralSessionId: root, ReturnOrigin: target), cancellationToken);
        return token is null ? null : auth + AuthenticationBrowserPaths.Handoff + "#" + token;
    }
}
