using Duende.IdentityServer.Services;
using Vitarish.Authentication.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure;

public sealed class DuendeLogoutContextService(IIdentityServerInteractionService interactionService) : ILogoutContextService
{
    public async Task<LogoutContext?> GetAsync(string? logoutId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logoutId))
            return new LogoutContext(null, null, null, true);

        Duende.IdentityServer.Models.LogoutRequest? request = await interactionService.GetLogoutContextAsync(logoutId);
        if (request is null)
            return null;

        return new LogoutContext(
            logoutId,
            request.ClientName,
            request.PostLogoutRedirectUri,
            request.ShowSignoutPrompt);
    }
}
