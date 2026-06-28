namespace Vitarish.Authentication.Contracts.Requests;

public sealed record BeginPasskeyAuthenticationRequest(string? UserNameOrEmail, string? ReturnUrl);
