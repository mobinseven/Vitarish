namespace Vitarish.Authentication.Application.Commands;

public sealed record CompleteEmailLoginCommand(string ChallengeId, string Code, string? ReturnUrl);
