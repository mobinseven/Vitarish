using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Security;

namespace Authentication.Tests.Api;

public class AuthenticationProblemHttpMapperTests
{
    [Theory]
    [InlineData(AuthenticationFailureReason.InvalidInput, StatusCodes.Status400BadRequest)]
    [InlineData(AuthenticationFailureReason.InvalidCredentials, StatusCodes.Status401Unauthorized)]
    [InlineData(AuthenticationFailureReason.ChallengeNotFound, StatusCodes.Status400BadRequest)]
    [InlineData(AuthenticationFailureReason.ChallengeExpired, StatusCodes.Status410Gone)]
    [InlineData(AuthenticationFailureReason.ChallengeAlreadyUsed, StatusCodes.Status400BadRequest)]
    [InlineData(AuthenticationFailureReason.ChallengeAttemptsExhausted, StatusCodes.Status429TooManyRequests)]
    [InlineData(AuthenticationFailureReason.InvalidVerificationCode, StatusCodes.Status400BadRequest)]
    [InlineData(AuthenticationFailureReason.InvalidChallengePurpose, StatusCodes.Status400BadRequest)]
    [InlineData(AuthenticationFailureReason.LockedOut, StatusCodes.Status423Locked)]
    [InlineData(AuthenticationFailureReason.NotAllowed, StatusCodes.Status403Forbidden)]
    [InlineData(AuthenticationFailureReason.TooManyAttempts, StatusCodes.Status429TooManyRequests)]
    [InlineData(AuthenticationFailureReason.AdditionalStepRequired, StatusCodes.Status409Conflict)]
    [InlineData(AuthenticationFailureReason.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(AuthenticationFailureReason.ProviderFailure, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(AuthenticationFailureReason.UnknownCredential, StatusCodes.Status400BadRequest)]
    [InlineData(AuthenticationFailureReason.InvalidCredential, StatusCodes.Status400BadRequest)]
    public void ToProblem_maps_reason_to_correct_status_code(AuthenticationFailureReason reason, int expectedStatus)
    {
        var failure = new AuthenticationFailure(reason, "test message");

        var result = AuthenticationProblemHttpMapper.ToProblem(failure);

        result.Should().NotBeNull();
        result.Should().BeOfType<ProblemHttpResult>();
        ((ProblemHttpResult)result).ProblemDetails.Status.Should().Be(expectedStatus);
    }
}
