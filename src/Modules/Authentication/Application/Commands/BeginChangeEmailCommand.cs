namespace Vitarish.Authentication.Application.Commands;

public sealed record BeginChangeEmailCommand(string UserId, string Email);
