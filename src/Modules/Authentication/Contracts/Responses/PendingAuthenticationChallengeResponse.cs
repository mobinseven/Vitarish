namespace Vitarish.Authentication.Contracts.Responses;

public sealed record PendingAuthenticationChallengeResponse(AuthenticationChallengeDto Challenge, string Destination, string? ReturnUrl,
    bool IsEmailLogin, bool IsPhoneLogin, bool IsPasswordOtp, bool IsPasswordRecovery, bool UsesEmailSecondFactor = false);
