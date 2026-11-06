using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Enums;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Api.Mapping;

public static class AuthenticationEndpointMapper
{
    public static AuthenticationChallengeDto ToDto(ChallengeResult result) => new(result.ChallengeId, ToDto(result.Purpose), result.ExpiresAt, result.RemainingAttempts, result.OptionsJson);

    public static AuthenticationChallengePurposeDto ToDto(AuthenticationChallengePurpose purpose) => purpose switch
    {
        AuthenticationChallengePurpose.PhoneLogin => AuthenticationChallengePurposeDto.PhoneLogin,
        AuthenticationChallengePurpose.EmailLogin => AuthenticationChallengePurposeDto.EmailLogin,
        AuthenticationChallengePurpose.PasswordLoginOtp => AuthenticationChallengePurposeDto.PasswordLoginOtp,
        AuthenticationChallengePurpose.PasswordLoginEmailOtp => AuthenticationChallengePurposeDto.PasswordLoginEmailOtp,
        AuthenticationChallengePurpose.PhoneChange => AuthenticationChallengePurposeDto.PhoneChange,
        AuthenticationChallengePurpose.EmailChange => AuthenticationChallengePurposeDto.EmailChange,
        AuthenticationChallengePurpose.EmailPasswordRecovery => AuthenticationChallengePurposeDto.EmailPasswordRecovery,
        AuthenticationChallengePurpose.PhonePasswordRecovery => AuthenticationChallengePurposeDto.PhonePasswordRecovery,
        _ => AuthenticationChallengePurposeDto.PasskeyLogin
    };
    public static AuthenticationRedirectResultDto ToDto(LoginResult result) => new(result.ReturnUrl, result.RequiresProfileCompletion);
    public static LogoutResultDto ToDto(LogoutResult result) => new(result.SignedOut, result.RedirectUrl);
}
