namespace Vitarish.Authentication.Application.Commands;

public sealed record CompletePasswordRecoveryCommand(string ChallengeId, string Code, string NewPassword);
