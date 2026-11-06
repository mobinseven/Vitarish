namespace Vitarish.Authentication.Application.Commands;

public sealed record ChangeTwoStepSignInCommand(string UserId, bool Enabled);
