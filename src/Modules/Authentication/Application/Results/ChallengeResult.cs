using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Application.Results;

public sealed record ChallengeResult(string ChallengeId, AuthenticationChallengePurpose Purpose, DateTimeOffset ExpiresAt, int RemainingAttempts, string? OptionsJson = null);
