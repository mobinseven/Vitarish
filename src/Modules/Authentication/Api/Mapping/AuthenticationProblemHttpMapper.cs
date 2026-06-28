using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Errors;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Api.Mapping;

public static class AuthenticationProblemHttpMapper
{
    public static IResult ToProblem(AuthenticationFailure failure)
    {
        (int status, AuthenticationErrorCode code) = failure.Reason switch
        {
            AuthenticationFailureReason.InvalidInput => (StatusCodes.Status400BadRequest, AuthenticationErrorCode.invalid_input),
            AuthenticationFailureReason.InvalidCredentials => (StatusCodes.Status401Unauthorized, AuthenticationErrorCode.invalid_credentials),
            AuthenticationFailureReason.ChallengeNotFound => (StatusCodes.Status400BadRequest, AuthenticationErrorCode.challenge_not_found),
            AuthenticationFailureReason.ChallengeExpired => (StatusCodes.Status410Gone, AuthenticationErrorCode.challenge_expired),
            AuthenticationFailureReason.ChallengeAlreadyUsed => (StatusCodes.Status400BadRequest, AuthenticationErrorCode.challenge_already_used),
            AuthenticationFailureReason.ChallengeAttemptsExhausted => (StatusCodes.Status429TooManyRequests, AuthenticationErrorCode.challenge_attempts_exhausted),
            AuthenticationFailureReason.InvalidVerificationCode => (StatusCodes.Status400BadRequest, AuthenticationErrorCode.invalid_verification_code),
            AuthenticationFailureReason.InvalidChallengePurpose => (StatusCodes.Status400BadRequest, AuthenticationErrorCode.invalid_challenge_purpose),
            AuthenticationFailureReason.LockedOut => (StatusCodes.Status423Locked, AuthenticationErrorCode.locked_out),
            AuthenticationFailureReason.NotAllowed => (StatusCodes.Status403Forbidden, AuthenticationErrorCode.not_allowed),
            AuthenticationFailureReason.TooManyAttempts => (StatusCodes.Status429TooManyRequests, AuthenticationErrorCode.too_many_attempts),
            AuthenticationFailureReason.AdditionalStepRequired => (StatusCodes.Status409Conflict, AuthenticationErrorCode.additional_step_required),
            AuthenticationFailureReason.Unauthorized => (StatusCodes.Status401Unauthorized, AuthenticationErrorCode.unauthorized),
            AuthenticationFailureReason.ProviderFailure => (StatusCodes.Status503ServiceUnavailable, AuthenticationErrorCode.provider_failure),
            AuthenticationFailureReason.UnknownCredential => (StatusCodes.Status400BadRequest, AuthenticationErrorCode.unknown_credential),
            AuthenticationFailureReason.InvalidCredential => (StatusCodes.Status400BadRequest, AuthenticationErrorCode.invalid_credential),
            _ => (StatusCodes.Status500InternalServerError, AuthenticationErrorCode.invalid_input)
        };

        return Results.Problem(new ProblemDetails
        {
            Status = status,
            Title = failure.Reason.ToString(),
            Detail = failure.Message,
            Extensions = { ["code"] = code.ToString() }
        });
    }
}
