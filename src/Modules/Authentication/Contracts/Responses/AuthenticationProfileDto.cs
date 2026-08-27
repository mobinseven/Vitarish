namespace Vitarish.Authentication.Contracts.Responses;

public sealed record AuthenticationProfileDto(
    bool AllowPasswordlessPhoneLogin,
    bool AllowPasswordlessEmailLogin,
    bool AllowPasskeyLogin);
