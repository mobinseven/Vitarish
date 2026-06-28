namespace Vitarish.Authentication.Application.Commands;

public sealed record BeginEmailLoginCommand(string Email, string? ReturnUrl);
