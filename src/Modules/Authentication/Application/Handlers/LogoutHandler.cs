using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Results;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class LogoutHandler(IAuthenticationSignInService signInService, ILogoutContextService logoutContexts)
{
    public async Task<AuthenticationResult<LogoutResult>> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        await signInService.SignOutAsync(cancellationToken);

        string? redirectUrl = null;
        if (!string.IsNullOrWhiteSpace(command.LogoutId))
        {
            LogoutContext? context = await logoutContexts.GetAsync(command.LogoutId, cancellationToken);
            redirectUrl = context?.PostLogoutRedirectUri;
        }

        redirectUrl ??= command.ReturnUrl;
        return new AuthenticationResult<LogoutResult>.Success(new(true, redirectUrl));
    }
}
