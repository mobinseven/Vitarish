using Microsoft.Extensions.Logging;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Parties.Application.Onboarding;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class AuthenticationTenantHandoffIssuer(
    IssueTenantHandoffHandler handler,
    ILogger<AuthenticationTenantHandoffIssuer> logger) : ITenantHandoffIssuer
{
    public Task<string?> IssueAsync(string userId, string hostname, CancellationToken cancellationToken = default) =>
        IssueAsync(new IssueTenantHandoffCommand(userId, hostname), cancellationToken);

    public Task<string?> IssueEndingSessionAsync(string userId, string hostname, CancellationToken cancellationToken = default) =>
        IssueAsync(new IssueTenantHandoffCommand(userId, hostname, EndsIssuingSession: true), cancellationToken);

    private async Task<string?> IssueAsync(IssueTenantHandoffCommand command, CancellationToken cancellationToken)
    {
        try
        {
            return await handler.Handle(command, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Without a ticket the user signs in on the target host directly.
            logger.LogWarning(exception, "Could not issue a handoff ticket for {Hostname}.", command.TargetHost);
            return null;
        }
    }
}
