namespace Vitarish.Authentication.Application.Commands;

public sealed record LoginWithPasswordCommand(string UserNameOrEmail, string Password, string? ReturnUrl, bool RememberMe);
