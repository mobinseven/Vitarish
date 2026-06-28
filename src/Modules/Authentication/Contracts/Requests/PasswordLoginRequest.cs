namespace Vitarish.Authentication.Contracts.Requests;

public sealed record PasswordLoginRequest(string UserNameOrEmail, string Password, string? ReturnUrl, bool RememberMe = false);
