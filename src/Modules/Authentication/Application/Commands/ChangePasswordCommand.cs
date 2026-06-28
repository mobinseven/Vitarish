namespace Vitarish.Authentication.Application.Commands;

public sealed record ChangePasswordCommand(string UserId, string CurrentPassword, string NewPassword);
