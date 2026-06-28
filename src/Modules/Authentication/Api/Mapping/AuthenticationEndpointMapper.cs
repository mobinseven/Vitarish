using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Enums;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Authentication.Domain.Challenges;

namespace Vitarish.Authentication.Api.Mapping;

public static class AuthenticationEndpointMapper
{
    public static AuthenticationChallengeDto ToDto(ChallengeResult result) => new(result.ChallengeId, result.Purpose switch { AuthenticationChallengePurpose.PhoneLogin => AuthenticationChallengePurposeDto.PhoneLogin, AuthenticationChallengePurpose.EmailLogin => AuthenticationChallengePurposeDto.EmailLogin, _ => AuthenticationChallengePurposeDto.PasskeyLogin }, result.ExpiresAt, result.RemainingAttempts);
    public static AuthenticationRedirectResultDto ToDto(LoginResult result) => new(result.ReturnUrl, result.RequiresProfileCompletion);
    public static LogoutResultDto ToDto(LogoutResult result) => new(result.SignedOut, result.RedirectUrl);
}
