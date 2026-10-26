using MediatR;
using Microsoft.Extensions.Options;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Parties.Application.SignIn;

/// <summary>A clinic the signed-in user can open, at its primary host.</summary>
/// <param name="Role">The user's tenant role suffix, such as <c>TenantOwner</c>.</param>
public sealed record SignInClinic(Guid TenantId, string Name, string Hostname, string Role, bool HasLogo);

/// <summary>Reads the clinics a user holds a tenant role in.</summary>
public interface ISignInClinicDirectory
{
    /// <returns>Live tenants with an active primary host, at most one entry per tenant.</returns>
    Task<IReadOnlyList<SignInClinic>> ListAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>The signed-in user's clinics, ordered by name.</summary>
public sealed record ListSignInClinicsQuery : IRequest<IReadOnlyList<SignInClinic>?>;

public sealed class ListSignInClinicsQueryHandler(ISignInClinicDirectory clinics, IRequestContext requestContext)
    : IRequestHandler<ListSignInClinicsQuery, IReadOnlyList<SignInClinic>?>
{
    /// <returns>Null when the request has no signed-in user.</returns>
    public async Task<IReadOnlyList<SignInClinic>?> Handle(ListSignInClinicsQuery request, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(requestContext.UserId)
            ? null
            : [.. (await clinics.ListAsync(requestContext.UserId, cancellationToken)).OrderBy(clinic => clinic.Name, StringComparer.CurrentCulture)];
}

/// <summary>A one-time link target: continue signed in at <paramref name="Hostname"/> with <paramref name="Token"/>.</summary>
public sealed record SignInHandoff(string Hostname, string Token);

/// <summary>
/// Issues a handoff link from the sign-in host to one of the user's clinics, or to the
/// onboarding host when <paramref name="TenantId"/> is null. The target is always a host the
/// server chose from the user's own clinics or its configuration, never one the caller names.
/// The sign-in host keeps no session of its own: issuing the link signs the user out there, so
/// signing out on the target host leaves nothing signed in behind it.
/// </summary>
public sealed record IssueSignInHandoffCommand(Guid? TenantId) : IRequest<SignInHandoff?>;

public sealed class IssueSignInHandoffCommandHandler(
    ISignInClinicDirectory clinics,
    ITenantHandoffIssuer handoffs,
    IOptions<OnboardingHostOptions> onboarding,
    IRequestContext requestContext)
    : IRequestHandler<IssueSignInHandoffCommand, SignInHandoff?>
{
    /// <returns>Null when there is no signed-in user, the clinic is not theirs, onboarding is off or has no host, or no ticket could be issued.</returns>
    public async Task<SignInHandoff?> Handle(IssueSignInHandoffCommand command, CancellationToken cancellationToken)
    {
        string? userId = requestContext.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        string? hostname = command.TenantId is { } tenantId
            ? (await clinics.ListAsync(userId, cancellationToken)).FirstOrDefault(clinic => clinic.TenantId == tenantId)?.Hostname
            : onboarding.Value.TenantOnboardingEnabled ? OnboardingHostname(onboarding.Value.OnboardingUrl) : null;
        if (hostname is null)
            return null;

        string? token = await handoffs.IssueEndingSessionAsync(userId, hostname, cancellationToken);
        return token is null ? null : new SignInHandoff(hostname, token);
    }

    private static string? OnboardingHostname(string? onboardingUrl) =>
        Uri.TryCreate(onboardingUrl, UriKind.Absolute, out Uri? uri) ? uri.Host : null;
}
