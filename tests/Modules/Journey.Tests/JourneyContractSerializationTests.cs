using System.Text.Json;
using Vitarish.Journey.Contracts.Responses;

namespace Journey.Tests;

public class JourneyContractSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void JourneyDecisionDto_round_trip()
    {
        var step = new JourneyStepDto(
            "/account/profile/complete",
            JourneyDestinationKindDto.Gate,
            new JourneyGateDto(JourneyGateKindDto.ProfileCompletion, true, "/account/profile/complete", "Missing fields"));

        var original = new JourneyDecisionDto(step, "/dashboard", false);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<JourneyDecisionDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Step.DestinationUrl.Should().Be("/account/profile/complete");
        deserialized.Step.DestinationKind.Should().Be(JourneyDestinationKindDto.Gate);
        deserialized.Step.BlockingGate.Should().NotBeNull();
        deserialized.Step.BlockingGate!.Kind.Should().Be(JourneyGateKindDto.ProfileCompletion);
        deserialized.Step.BlockingGate.IsBlocking.Should().BeTrue();
        deserialized.LegacyReturnUrl.Should().Be("/dashboard");
        deserialized.LegacyRequiresProfileCompletion.Should().BeFalse();
    }

    [Fact]
    public void JourneyDecisionDto_round_trip_without_legacy_fields()
    {
        var step = new JourneyStepDto(
            "/home",
            JourneyDestinationKindDto.UserHome);

        var original = new JourneyDecisionDto(step);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<JourneyDecisionDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Step.DestinationUrl.Should().Be("/home");
        deserialized.Step.DestinationKind.Should().Be(JourneyDestinationKindDto.UserHome);
        deserialized.LegacyReturnUrl.Should().BeNull();
        deserialized.LegacyRequiresProfileCompletion.Should().BeNull();
    }

    [Fact]
    public void JourneyStepDto_round_trip_with_non_blocking_gates()
    {
        var nonBlockingGates = new List<JourneyGateDto>
        {
            new(JourneyGateKindDto.PasskeyEnrollmentPrompt, false, "/account/passkeys", "Consider enrolling a passkey"),
            new(JourneyGateKindDto.MfaEnrollmentPrompt, false, "/account/mfa", "Consider enabling MFA")
        };

        var original = new JourneyStepDto(
            "/dashboard",
            JourneyDestinationKindDto.ReturnUrl,
            null,
            nonBlockingGates);

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<JourneyStepDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.NonBlockingGates.Should().HaveCount(2);
        deserialized.NonBlockingGates![0].Kind.Should().Be(JourneyGateKindDto.PasskeyEnrollmentPrompt);
        deserialized.NonBlockingGates[1].Kind.Should().Be(JourneyGateKindDto.MfaEnrollmentPrompt);
    }

    [Fact]
    public void JourneyGateKindDto_serializes_as_integer()
    {
        var json = JsonSerializer.Serialize(JourneyGateKindDto.ProfileCompletion, JsonOptions);
        json.Should().Be("2");
    }

    [Fact]
    public void JourneyDestinationKindDto_serializes_as_integer()
    {
        var json = JsonSerializer.Serialize(JourneyDestinationKindDto.AppDashboard, JsonOptions);
        json.Should().Be("3");
    }

    [Fact]
    public void JourneyGateDto_round_trip()
    {
        var original = new JourneyGateDto(
            JourneyGateKindDto.ContactConfirmation,
            true,
            "/account/contact/confirm",
            "At least one confirmed contact is required");

        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<JourneyGateDto>(json, JsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.Kind.Should().Be(JourneyGateKindDto.ContactConfirmation);
        deserialized.IsBlocking.Should().BeTrue();
        deserialized.DestinationUrl.Should().Be("/account/contact/confirm");
        deserialized.Message.Should().Be("At least one confirmed contact is required");
    }
}
