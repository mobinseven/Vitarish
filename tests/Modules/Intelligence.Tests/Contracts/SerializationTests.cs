using System.Text.Json;
using FluentAssertions;
using Vitarish.Intelligence.Contracts;

namespace Intelligence.Tests.Contracts;

public class SerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };

    [Fact]
    public void IntelligenceResult_success_round_trip()
    {
        var original = IntelligenceResult<string>.Success("ok");
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<IntelligenceResult<string>>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.Succeeded.Should().BeTrue();
        deserialized.Value.Should().Be("ok");
    }

    [Fact]
    public void IntelligenceResult_failure_round_trip()
    {
        var error = new IntelligenceError(IntelligenceErrorCode.ProviderUnavailable, "Provider down");
        var original = IntelligenceResult<string>.Failed(error);
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<IntelligenceResult<string>>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.Succeeded.Should().BeFalse();
        deserialized.Error!.Code.Should().Be(IntelligenceErrorCode.ProviderUnavailable);
    }

    [Fact]
    public void IntelligenceErrorCode_has_expected_values()
    {
        var values = Enum.GetValues<IntelligenceErrorCode>();
        values.Should().Contain(IntelligenceErrorCode.ValidationFailure);
        values.Should().Contain(IntelligenceErrorCode.Unauthorized);
        values.Should().Contain(IntelligenceErrorCode.Forbidden);
        values.Should().Contain(IntelligenceErrorCode.ProviderUnavailable);
        values.Should().Contain(IntelligenceErrorCode.ProviderTimeout);
        values.Should().Contain(IntelligenceErrorCode.Cancellation);
        values.Should().Contain(IntelligenceErrorCode.DeserializationFailure);
        values.Should().Contain(IntelligenceErrorCode.UnexpectedFailure);
    }

    [Fact]
    public void IntelligenceApiRoutes_constants_are_correct()
    {
        IntelligenceApiRoutes.Base.Should().Be("/api/modules/intelligence");
        IntelligenceApiRoutes.Responder.Should().Be("/responder");
        IntelligenceApiRoutes.Transcription.Should().Be("/transcription");
        IntelligenceApiRoutes.Rewrite.Should().Be("/rewrite");
        IntelligenceApiRoutes.LiveTranscription.Should().Be("/transcription/live");
        IntelligenceApiRoutes.LiveTranscriptionCapabilities.Should()
            .Be(IntelligenceApiRoutes.LiveTranscription + "/capabilities");
    }

    [Fact]
    public void ResponderInput_text_input_factory()
    {
        var input = ResponderInput.TextInput("hello");
        input.Type.Should().Be(ResponderInputType.Text);
        input.Text.Should().Be("hello");
    }

    [Fact]
    public void IntelligenceFileReference_round_trip()
    {
        var original = new IntelligenceFileReference { FileId = "file-1", FileName = "test.pdf", ContentType = "application/pdf", Size = 1024 };
        var json = JsonSerializer.Serialize(original, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<IntelligenceFileReference>(json, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.FileId.Should().Be("file-1");
        deserialized.FileName.Should().Be("test.pdf");
        deserialized.Size.Should().Be(1024);
    }

    [Fact]
    public void Realtime_transcription_capability_round_trip_exposes_only_client_safe_values()
    {
        var original = new RealtimeTranscriptionCapabilities(
            true,
            ["fa-IR", "en-US"],
            ["audio/webm;codecs=opus"],
            new RealtimeTranscriptionLimits(65_536, 12_582_912, 270, 20));

        string json = JsonSerializer.Serialize(original, JsonOptions);
        RealtimeTranscriptionCapabilities? deserialized =
            JsonSerializer.Deserialize<RealtimeTranscriptionCapabilities>(json, JsonOptions);

        deserialized.Should().BeEquivalentTo(original);
        json.Should().NotContain("Google");
        json.Should().NotContain("model");
        json.Should().NotContain("credential");
        json.Should().NotContain("project");
    }

    [Fact]
    public void Realtime_transcription_error_codes_serialize_as_stable_strings()
    {
        var message = new RealtimeTranscriptionServerMessage(
            RealtimeTranscriptionMessageTypes.Failed,
            "session-1",
            Error: new RealtimeTranscriptionError(
                RealtimeTranscriptionErrorCode.ProviderUnavailable,
                "Unavailable"));

        string json = JsonSerializer.Serialize(message, JsonOptions);

        json.Should().Contain("\"code\":\"ProviderUnavailable\"");
    }
}
