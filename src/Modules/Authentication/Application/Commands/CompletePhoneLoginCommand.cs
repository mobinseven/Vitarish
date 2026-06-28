namespace Vitarish.Authentication.Application.Commands;

public sealed record CompletePhoneLoginCommand(string ChallengeId, string Code, string? ReturnUrl);
