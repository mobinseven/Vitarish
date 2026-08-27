namespace Vitarish.Authentication.Application.Commands;

public sealed record CompletePasswordLoginOtpCommand(string ChallengeId, string Code, string? ReturnUrl);
