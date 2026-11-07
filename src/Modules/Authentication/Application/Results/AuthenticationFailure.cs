using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Results;

public sealed record AuthenticationFailure(
    AuthenticationFailureReason Reason,
    string Message,
    string? ChallengeId = null,
    AuthenticationChallengePurpose? ChallengePurpose = null,
    string? MaskedDestination = null,
    DateTimeOffset? ChallengeExpiresAt = null,
    int? RemainingAttempts = null);
