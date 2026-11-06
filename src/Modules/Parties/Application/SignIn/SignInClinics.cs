using MediatR;
using Microsoft.Extensions.Options;
using Vitarish.Journey.Application.Abstractions;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Application.SignIn;

/// <summary>A workspace the signed-in user can open, at its primary host.</summary>
/// <param name="Role">The user's tenant role suffix, such as <c>TenantOwner</c>.</param>
public sealed record WorkspaceDirectoryEntry(Guid TenantId, string AppId, string Name, string Hostname, string Role, bool HasLogo);

/// <summary>Reads the app workspaces a user holds a tenant role in.</summary>
public interface IWorkspaceDirectory
{
    /// <returns>Workspace tenants with an active primary host, at most one entry per tenant.</returns>
    Task<IReadOnlyList<WorkspaceDirectoryEntry>> ListAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>The signed-in user's clinics, ordered by name.</summary>
public sealed record ListSignInClinicsQuery : IRequest<IReadOnlyList<WorkspaceDirectoryEntry>?>;

public sealed class ListSignInClinicsQueryHandler(IWorkspaceDirectory workspaces, IRequestContext requestContext)
    : IRequestHandler<ListSignInClinicsQuery, IReadOnlyList<WorkspaceDirectoryEntry>?>
{
    /// <returns>Null when the request has no signed-in user.</returns>
    public async Task<IReadOnlyList<WorkspaceDirectoryEntry>?> Handle(ListSignInClinicsQuery request, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(requestContext.UserId)
            ? null
            : [.. (await workspaces.ListAsync(requestContext.UserId, cancellationToken)).OrderBy(workspace => workspace.Name, StringComparer.CurrentCulture)];
}

/// <summary>A one-time link target: continue signed in at <paramref name="Hostname"/> with <paramref name="Token"/>.</summary>
public sealed record SignInHandoff(string Hostname, string Token, string? ReturnTo = null);

public enum SignInHandoffTarget { WorkspaceOrOnboarding, Home }

/// <summary>
/// Issues a handoff to a workspace, onboarding, or the configured home host. The server
/// chooses the target host from the user's workspaces or configuration.
/// The sign-in host keeps no session of its own: issuing the link signs the user out there, so
/// signing out on the target host leaves nothing signed in behind it.
/// </summary>
public sealed record IssueSignInHandoffCommand(Guid? TenantId, SignInHandoffTarget Target = SignInHandoffTarget.WorkspaceOrOnboarding, string? ReturnTo = null) : IRequest<SignInHandoff?>;

public sealed class IssueSignInHandoffCommandHandler(
    IWorkspaceDirectory clinics,
    ITenantHandoffIssuer handoffs,
    IOptions<OnboardingHostOptions> onboarding,
    IOptions<SignInHomeOptions> home,
    IJourneyReturnUrlPolicy returnUrlPolicy,
    IRequestContext requestContext)
    : IRequestHandler<IssueSignInHandoffCommand, SignInHandoff?>
{
    /// <returns>Null when there is no signed-in user, the clinic is not theirs, onboarding is off or has no host, or no ticket could be issued.</returns>
    public async Task<SignInHandoff?> Handle(IssueSignInHandoffCommand command, CancellationToken cancellationToken)
    {
        string? userId = requestContext.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        string? hostname = command.Target switch
        {
            SignInHandoffTarget.Home when command.TenantId is null => ConfiguredHostname(home.Value.LandingUrl),
            SignInHandoffTarget.WorkspaceOrOnboarding when command.TenantId is { } tenantId =>
                (await clinics.ListAsync(userId, cancellationToken)).FirstOrDefault(clinic => clinic.TenantId == tenantId)?.Hostname,
            SignInHandoffTarget.WorkspaceOrOnboarding when onboarding.Value.IsAvailable => onboarding.Value.Hostname,
            _ => null,
        };
        if (hostname is null)
            return null;

        string? returnTo = command.Target == SignInHandoffTarget.Home ? returnUrlPolicy.Normalize(command.ReturnTo) : null;
        string? token = await handoffs.IssueEndingSessionAsync(userId, hostname, cancellationToken);
        return token is null ? null : new SignInHandoff(hostname, token, returnTo);
    }

    private static string? ConfiguredHostname(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ? uri.Host : null;
}
