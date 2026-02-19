using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Text.Encodings.Web;

namespace Main.Tests;

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Context.User?.Identities.Any(identity => identity.IsAuthenticated) == true)
        {
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(Context.User, Scheme.Name)));
        }

        if (Context.User?.Claims.Any() == true)
        {
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(Context.User, Scheme.Name)));
        }

        return Task.FromResult(AuthenticateResult.NoResult());
    }
}
