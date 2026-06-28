namespace Vitarish.Authentication.Contracts.Requests;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
