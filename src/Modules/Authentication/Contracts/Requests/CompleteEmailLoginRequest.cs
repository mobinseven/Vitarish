namespace Vitarish.Authentication.Contracts.Requests;

public sealed record CompleteEmailLoginRequest(string ChallengeId, string Code, string? ReturnUrl);
