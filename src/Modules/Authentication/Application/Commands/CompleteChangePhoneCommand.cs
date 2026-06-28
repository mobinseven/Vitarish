namespace Vitarish.Authentication.Application.Commands;

public sealed record CompleteChangePhoneCommand(string UserId, string ChallengeId, string Code);
