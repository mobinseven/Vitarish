namespace Vitarish.Authentication.Contracts.Requests;

public sealed record CompleteChangeEmailRequest(string ChallengeId, string Code);
