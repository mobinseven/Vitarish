using FluentAssertions;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Enums;
using Vitarish.Authentication.Domain.Challenges;

namespace Authentication.Tests.Api;

public class AuthenticationEndpointMapperTests
{
    [Fact]
    public void ToDto_challenge_result_maps_all_fields()
    {
        var now = DateTimeOffset.UtcNow;
        var result = new ChallengeResult(
            "challenge-123",
            AuthenticationChallengePurpose.EmailLogin,
            now.AddMinutes(5),
            3,
            "options-json");

        var dto = AuthenticationEndpointMapper.ToDto(result);

        dto.ChallengeId.Should().Be("challenge-123");
        dto.Purpose.Should().Be(AuthenticationChallengePurposeDto.EmailLogin);
        dto.ExpiresAt.Should().Be(now.AddMinutes(5));
        dto.RemainingAttempts.Should().Be(3);
        dto.OptionsJson.Should().Be("options-json");
    }

    [Fact]
    public void ToDto_challenge_result_maps_phone_purpose()
    {
        var result = new ChallengeResult(
            "id",
            AuthenticationChallengePurpose.PhoneLogin,
            DateTimeOffset.UtcNow,
            5);

        var dto = AuthenticationEndpointMapper.ToDto(result);

        dto.Purpose.Should().Be(AuthenticationChallengePurposeDto.PhoneLogin);
    }

    [Fact]
    public void ToDto_challenge_result_maps_passkey_purpose()
    {
        var result = new ChallengeResult(
            "id",
            AuthenticationChallengePurpose.PasskeyLogin,
            DateTimeOffset.UtcNow,
            5);

        var dto = AuthenticationEndpointMapper.ToDto(result);

        dto.Purpose.Should().Be(AuthenticationChallengePurposeDto.PasskeyLogin);
    }

    [Fact]
    public void ToDto_login_result_maps_all_fields()
    {
        var result = new LoginResult("user-1", "/dashboard", true);

        var dto = AuthenticationEndpointMapper.ToDto(result);

        dto.ReturnUrl.Should().Be("/dashboard");
        dto.RequiresProfileCompletion.Should().BeTrue();
    }

    [Fact]
    public void ToDto_logout_result_maps_signed_out()
    {
        var result = new LogoutResult(true, "/");

        var dto = AuthenticationEndpointMapper.ToDto(result);

        dto.SignedOut.Should().BeTrue();
        dto.RedirectUrl.Should().Be("/");
    }

    [Fact]
    public void ToDto_logout_result_maps_not_signed_out()
    {
        var result = new LogoutResult(false, null);

        var dto = AuthenticationEndpointMapper.ToDto(result);

        dto.SignedOut.Should().BeFalse();
        dto.RedirectUrl.Should().BeNull();
    }
}
