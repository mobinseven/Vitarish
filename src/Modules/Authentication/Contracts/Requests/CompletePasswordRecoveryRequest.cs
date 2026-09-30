namespace Vitarish.Authentication.Contracts.Requests;

public sealed record CompletePasswordRecoveryRequest(string ChallengeId, string Code, string NewPassword);
