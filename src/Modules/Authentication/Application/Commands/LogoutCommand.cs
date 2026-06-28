namespace Vitarish.Authentication.Application.Commands;

public sealed record LogoutCommand(string? LogoutId, string? ReturnUrl);
