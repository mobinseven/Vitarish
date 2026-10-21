using MediatR;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Admits a session without a role only on the two platform hosts that need one: the onboarding
/// host, where new accounts complete their profile and create their clinic, and the shared
/// sign-in host, where people choose which of their clinics to continue to. Neither tenant holds
/// practice data, and a role-free session has no access to it or any other tenant.
/// </summary>
public sealed class PlatformHostRoleFreeSessionPolicy(ISender sender) : IRoleFreeSessionPolicy
{
    public async Task<bool> AllowsRoleFreeSessionAsync(CancellationToken cancellationToken = default) =>
        await sender.Send(new IsOnboardingHostQuery(), cancellationToken)
        || await sender.Send(new IsSignInHostQuery(), cancellationToken);
}
