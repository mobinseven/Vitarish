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
    IReadOnlySet<FailedAttemptResponse> AvailableFailedAttemptResponses);
