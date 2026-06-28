namespace Vitarish.Authentication.Contracts.Requests;

public sealed record CompleteChangePhoneRequest(string ChallengeId, string Code);
