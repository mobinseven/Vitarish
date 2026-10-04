using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Application.Results;

public sealed record SecurityAuthenticationPolicyManagementState(
    SecurityAccessPolicyState State,
    IReadOnlySet<AuthenticationMethod> AvailableMechanisms,
    IReadOnlySet<AuthenticationMethod> RequiredMechanisms,
    IReadOnlySet<PreAuthenticationAction> AvailableActions,
    IReadOnlySet<PreAuthenticationAction> RequiredActions,
    IReadOnlySet<FailedAttemptResponse> AvailableFailedAttemptResponses)
{
    public static SecurityAuthenticationPolicyManagementState ForState(
        SecurityAccessPolicyState state,
        AuthenticationProfilePolicy profilePolicy) => new(
            state with { Policy = state.Policy.ReconcileProfile(profilePolicy) },
            profilePolicy.EnabledAuthenticationMechanisms,
            profilePolicy.RequiredAuthenticationMechanisms,
            profilePolicy.PermittedPreAuthenticationActions,
            profilePolicy.RequiredPreAuthenticationActions,
            Enum.GetValues<FailedAttemptResponse>().ToHashSet());
}
