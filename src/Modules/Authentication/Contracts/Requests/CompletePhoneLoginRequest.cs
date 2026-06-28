namespace Vitarish.Authentication.Contracts.Requests;

public sealed record CompletePhoneLoginRequest(string ChallengeId, string Code, string? ReturnUrl);
