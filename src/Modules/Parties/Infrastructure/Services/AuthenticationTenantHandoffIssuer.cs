using Microsoft.Extensions.Logging;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Parties.Application.Onboarding;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class AuthenticationTenantHandoffIssuer(
    IssueTenantHandoffHandler handler,
    ILogger<AuthenticationTenantHandoffIssuer> logger) : ITenantHandoffIssuer
{
    public async Task<string?> IssueAsync(string userId, string hostname, CancellationToken cancellationToken = default)
    {
        try
        {
            return await handler.Handle(new IssueTenantHandoffCommand(userId, hostname), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The tenant exists either way; without a ticket its creator signs in on the new host.
            logger.LogWarning(exception, "Could not issue a handoff ticket for the new tenant at {Hostname}.", hostname);
            return null;
        }
    }
}
