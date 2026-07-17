using FluentAssertions;
using Google.Cloud.Speech.V2;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.LiveTranscription;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Services.Google;

namespace Intelligence.Tests.Infrastructure;

public sealed class GoogleLiveTranscriptionProviderTests
{
    [Fact]
    public void Provider_is_available_with_adc_and_rejects_placeholder_configuration()
    {
        GoogleLiveTranscriptionProvider configured = CreateProvider(new GoogleSpeechOptions
        {
            ProjectId = "clinic-project",
            Location = "us-central1",
            CredentialsPath = null
        });
        GoogleLiveTranscriptionProvider placeholder = CreateProvider(new GoogleSpeechOptions
        {
            ProjectId = "google-project-id",
            Location = "us-central1",
            CredentialsPath = null
        });

        configured.IsAvailable.Should().BeTrue();
        placeholder.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void Streaming_config_uses_implicit_recognizer_chirp_2_and_requested_language()
    {
        GoogleSpeechOptions options = new()
        {
            ProjectId = "clinic-project",
            Location = "us-central1"
        };

        string recognizer = GoogleLiveTranscriptionProvider.BuildRecognizerName(options);
        Google.Cloud.Speech.V2.StreamingRecognitionConfig config =
            GoogleLiveTranscriptionProvider.CreateStreamingConfig("fa-IR");

        recognizer.Should().Be("projects/clinic-project/locations/us-central1/recognizers/_");
        config.Config.Model.Should().Be("chirp_2");
        config.Config.LanguageCodes.Should().Equal("fa-IR");
        config.Config.AutoDecodingConfig.Should().NotBeNull();
    }

    [Fact]
    public void Provider_chunks_never_exceed_the_google_streaming_request_limit()
    {
        byte[] frame = Enumerable.Range(0, GoogleLiveTranscriptionProvider.MaxProviderAudioBytes * 2 + 7)
            .Select(value => (byte)(value % 251))
            .ToArray();

        ArraySegment<byte>[] chunks =
            GoogleLiveTranscriptionProvider.EnumerateProviderChunks(frame).ToArray();

        chunks.Select(chunk => chunk.Count).Should().Equal(
            GoogleLiveTranscriptionProvider.MaxProviderAudioBytes,
            GoogleLiveTranscriptionProvider.MaxProviderAudioBytes,
            7);
        chunks.SelectMany(chunk => chunk).Should().Equal(frame);
    }

    [Fact]
    public void Provider_publishes_only_nonempty_final_segments()
    {
        StreamingRecognizeResponse response = new()
        {
            Results =
            {
                new StreamingRecognitionResult
                {
                    IsFinal = false,
                    Alternatives = { new SpeechRecognitionAlternative { Transcript = "interim" } }
                },
                new StreamingRecognitionResult
                {
                    IsFinal = true,
                    Alternatives = { new SpeechRecognitionAlternative { Transcript = "  confirmed  " } }
                },
                new StreamingRecognitionResult
                {
                    IsFinal = true,
                    Alternatives = { new SpeechRecognitionAlternative { Transcript = "   " } }
                }
            }
        };

        GoogleLiveTranscriptionProvider.EnumerateFinalSegments(response)
            .Should().Equal("confirmed");
    }

    [Theory]
    [InlineData(StatusCode.DeadlineExceeded, LiveTranscriptionErrorCode.ProviderTimeout)]
    [InlineData(StatusCode.InvalidArgument, LiveTranscriptionErrorCode.InvalidAudio)]
    [InlineData(StatusCode.Unavailable, LiveTranscriptionErrorCode.ProviderUnavailable)]
    [InlineData(StatusCode.ResourceExhausted, LiveTranscriptionErrorCode.ProviderUnavailable)]
    public void Provider_failures_map_to_stable_application_errors(
        StatusCode status,
        LiveTranscriptionErrorCode expected)
    {
        TranscriptionProviderException exception =
            GoogleLiveTranscriptionProvider.CreateProviderException(status);

        exception.Code.Should().Be(expected);
        exception.Message.Should().NotContain(status.ToString());
    }

    private static GoogleLiveTranscriptionProvider CreateProvider(GoogleSpeechOptions speech) =>
        new(Options.Create(new AiOptions
        {
            Google = new GoogleOptions { Speech = speech }
        }));
}
