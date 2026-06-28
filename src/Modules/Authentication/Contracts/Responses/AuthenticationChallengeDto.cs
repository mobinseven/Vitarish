using Vitarish.Authentication.Contracts.Enums;

namespace Vitarish.Authentication.Contracts.Responses;

public sealed record AuthenticationChallengeDto(string ChallengeId, AuthenticationChallengePurposeDto Purpose, DateTimeOffset ExpiresAt, int RemainingAttempts, string? OptionsJson = null);
