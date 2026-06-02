using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Vitarish.Main.Mock.Authentication;

public sealed class PreviewAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder), IAuthenticationSignOutHandler
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ClaimsPrincipal principal = PreviewAuthenticationDefaults.CreatePrincipal(Scheme.Name);
        AuthenticationTicket ticket = new(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    public Task SignOutAsync(AuthenticationProperties? properties) => Task.CompletedTask;
}
