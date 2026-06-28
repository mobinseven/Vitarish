namespace Vitarish.Authentication.Application.Commands;

public sealed record BeginPasskeyLoginCommand(string? UserNameOrEmail, string? ReturnUrl);
