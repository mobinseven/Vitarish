using System.Text.Json;
using FluentAssertions;
using Vitarish.Authentication.Contracts.Enums;
using Vitarish.Authentication.Contracts.Errors;
using Vitarish.Authentication.Contracts.Responses;

namespace Authentication.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void RedirectResultDto_round_trip()
    {
        var original = new AuthenticationRedirectResultDto("/dashboard", true);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AuthenticationRedirectResultDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.ReturnUrl.Should().Be("/dashboard");
        deserialized.RequiresProfileCompletion.Should().BeTrue();
    }

    [Fact]
    public void ChallengeDto_round_trip()
    {
        var now = DateTimeOffset.UtcNow;
        var original = new AuthenticationChallengeDto(
            "challenge-123",
            AuthenticationChallengePurposeDto.EmailLogin,
            now,
            3,
            "options");

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AuthenticationChallengeDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.ChallengeId.Should().Be("challenge-123");
        deserialized.Purpose.Should().Be(AuthenticationChallengePurposeDto.EmailLogin);
        deserialized.RemainingAttempts.Should().Be(3);
        deserialized.OptionsJson.Should().Be("options");
    }

    [Fact]
    public void SessionDto_round_trip_with_user()
    {
        var user = new CurrentAuthenticationUserDto("user-1", "John", "john@test.com", "+1234567890");
        var original = new AuthenticationSessionDto(true, user);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AuthenticationSessionDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.IsAuthenticated.Should().BeTrue();
        deserialized.User.Should().NotBeNull();
        deserialized.User!.UserId.Should().Be("user-1");
        deserialized.User.DisplayName.Should().Be("John");
    }

    [Fact]
    public void SessionDto_round_trip_without_user()
    {
        var original = new AuthenticationSessionDto(false, null);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AuthenticationSessionDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.IsAuthenticated.Should().BeFalse();
        deserialized.User.Should().BeNull();
    }

    [Fact]
    public void ProblemDto_round_trip()
    {
        var original = new AuthenticationProblemDto(
            AuthenticationErrorCode.invalid_credentials,
            "InvalidCredentials",
            "Bad credentials",
            401);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AuthenticationProblemDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Code.Should().Be(AuthenticationErrorCode.invalid_credentials);
        deserialized.Title.Should().Be("InvalidCredentials");
        deserialized.Status.Should().Be(401);
    }

    [Fact]
    public void LogoutResultDto_round_trip()
    {
        var original = new LogoutResultDto(true, "/home");

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<LogoutResultDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.SignedOut.Should().BeTrue();
        deserialized.RedirectUrl.Should().Be("/home");
    }

    [Fact]
    public void LogoutContextDto_round_trip()
    {
        var original = new LogoutContextDto("logout-1", "MyApp", "/logged-out", true);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<LogoutContextDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.LogoutId.Should().Be("logout-1");
        deserialized.ClientName.Should().Be("MyApp");
        deserialized.PostLogoutRedirectUri.Should().Be("/logged-out");
        deserialized.ShowLogoutPrompt.Should().BeTrue();
    }

    [Fact]
    public void ChallengePurposeDto_serializes_as_integer()
    {
        var json = JsonSerializer.Serialize(AuthenticationChallengePurposeDto.EmailLogin, JsonOptions);

        json.Should().Be("1");
    }

    [Fact]
    public void AuthenticationErrorCode_serializes_as_integer()
    {
        var json = JsonSerializer.Serialize(AuthenticationErrorCode.provider_failure, JsonOptions);

        json.Should().Be("13");
    }
}
