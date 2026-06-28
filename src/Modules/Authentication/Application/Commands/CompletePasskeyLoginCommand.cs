namespace Vitarish.Authentication.Application.Commands;

public sealed record CompletePasskeyLoginCommand(string ChallengeId, string CredentialJson, string? ReturnUrl);
