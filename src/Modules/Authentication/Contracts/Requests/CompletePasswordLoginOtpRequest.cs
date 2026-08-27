namespace Vitarish.Authentication.Contracts.Requests;

public sealed record CompletePasswordLoginOtpRequest(string ChallengeId, string Code, string? ReturnUrl);
