namespace Vitarish.Authentication.Application.Commands;

public sealed record CompleteChangeEmailCommand(string UserId, string ChallengeId, string Code);
