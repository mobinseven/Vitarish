namespace Vitarish.Authentication.Application.Commands;

public sealed record SetFirstPasswordCommand(string UserId, string NewPassword);
