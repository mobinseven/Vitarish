namespace Vitarish.Authentication.Contracts.Requests;

public sealed record CompletePasskeyAuthenticationRequest(string ChallengeId, string CredentialJson, string? ReturnUrl);
