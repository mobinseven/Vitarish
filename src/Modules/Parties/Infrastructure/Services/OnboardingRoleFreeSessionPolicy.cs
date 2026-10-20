using MediatR;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Parties.Application.Onboarding;

namespace Vitarish.Parties.Infrastructure.Services;

/// <summary>
/// Admits a session without a role only on the onboarding host, where new accounts complete
/// their profile and create their clinic. Its tenant holds no practice data, and a role-free
/// session has no access to it or any other tenant.
/// </summary>
public sealed class OnboardingRoleFreeSessionPolicy(ISender sender) : IRoleFreeSessionPolicy
{
    public Task<bool> AllowsRoleFreeSessionAsync(CancellationToken cancellationToken = default) =>
        sender.Send(new IsOnboardingHostQuery(), cancellationToken);
}
