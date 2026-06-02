using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Main.Tests;

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Context.User?.Identities.Any(identity => identity.IsAuthenticated) == true
            ? Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(Context.User, Scheme.Name)))
            : Context.User?.Claims.Any() == true
            ? Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(Context.User, Scheme.Name)))
            : Task.FromResult(AuthenticateResult.NoResult());
}
