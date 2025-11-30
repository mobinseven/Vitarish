
using global::Google.Api.Gax.Grpc;
using global::Google.Apis.Auth.OAuth2;
using global::Google.Cloud.Speech.V2;
using global::Google.Protobuf;
using global::Google.Protobuf.WellKnownTypes;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Vorn.Ai.Server.Domain.Options;
using Vorn.Ai.Server.Infrastructure.Interfaces;

namespace Vorn.Ai.Server.Infrastructure.Services.Google;

public sealed class GoogleTranscriber(VornOptions vornOptions) : ITranscriber
{
    private readonly GoogleSpeechOptions options = vornOptions.Ai.Google.Speech ?? new GoogleSpeechOptions();
    private readonly ConcurrentDictionary<string, string> modelResourceNames = new(StringComparer.OrdinalIgnoreCase);
    private SpeechClient? client;

    public async Task<string?> TranscribeAsync(Stream audio, string mimeType, string? language, string? modelId, CancellationToken cancellationToken)
    {
        SpeechClient speechClient = await EnsureClientAsync(cancellationToken).ConfigureAwait(false);
        string recognizer = BuildRecognizerName();
        RecognitionConfig config = CreateConfig(language, modelId);
        FieldMask configMask = CreateConfigMask(language, modelId);

        using MemoryStream buffer = new();
        await audio.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        RecognizeResponse response = await speechClient.RecognizeAsync(
            recognizer,
            config,
            configMask,
            ByteString.FromStream(buffer),
            cancellationToken).ConfigureAwait(false);

        return BuildTranscript(response.Results.SelectMany(r => r.Alternatives));
    }

    public async IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio,
        string mimeType,
        string? language,
        string? modelId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        SpeechClient speechClient = await EnsureClientAsync(cancellationToken).ConfigureAwait(false);
        string recognizer = BuildRecognizerName();
        RecognitionConfig config = CreateConfig(language, modelId);
        FieldMask configMask = CreateConfigMask(language, modelId);

        using SpeechClient.StreamingRecognizeStream call = speechClient.StreamingRecognize(callSettings: CallSettings.FromCancellationToken(cancellationToken));

        StreamingRecognitionConfig streamingConfig = new()
        {
            Config = config,
            ConfigMask = configMask
        };

        await call.WriteAsync(new StreamingRecognizeRequest
        {
            Recognizer = recognizer,
            StreamingConfig = streamingConfig
        }).ConfigureAwait(false);

        Task sendTask = Task.Run(async () =>
        {
            await foreach (byte[] chunk in audio.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                if (chunk.Length == 0)
                {
                    continue;
                }

                await call.WriteAsync(new StreamingRecognizeRequest
                {
                    Audio = ByteString.CopyFrom(chunk)
                }).ConfigureAwait(false);
            }

            await call.WriteCompleteAsync().ConfigureAwait(false);
        }, cancellationToken);

        AsyncResponseStream<StreamingRecognizeResponse> responseStream = call.GetResponseStream();
        await foreach(StreamingRecognizeResponse response in responseStream.WithCancellation(cancellationToken))
        {
            foreach(StreamingRecognitionResult result in response.Results)
            {
                if(!result.IsFinal)
                {
                    continue;
                }

                string? text = BuildTranscript(result.Alternatives);
                if(!string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }
        }

        await sendTask.ConfigureAwait(false);
    }

    private async Task<SpeechClient> EnsureClientAsync(CancellationToken cancellationToken)
    {
        if(client is not null)
        {
            return client;
        }

        SpeechClientBuilder builder = new()
        {
            Endpoint = string.IsNullOrWhiteSpace(options.Location)
                ? SpeechClient.DefaultEndpoint
                : $"{options.Location}-speech.googleapis.com"
        };

        if(!string.IsNullOrWhiteSpace(options.CredentialsPath))
        {
#pragma warning disable CS0618
            builder.GoogleCredential = GoogleCredential.FromFile(options.CredentialsPath);
#pragma warning restore CS0618
        }

        client = await builder.BuildAsync(cancellationToken).ConfigureAwait(false);
        return client;
    }

    private RecognitionConfig CreateConfig(string? language, string? modelId)
    {
        string resolvedLanguage = string.IsNullOrWhiteSpace(language) ? options.DefaultLanguageCode : language!;
        string modelResource = ResolveModel(modelId);

        RecognitionConfig config = new()
        {
            Model = modelResource,
            AutoDecodingConfig = new AutoDetectDecodingConfig()
        };

        if(!string.IsNullOrWhiteSpace(resolvedLanguage))
        {
            config.LanguageCodes.Add(resolvedLanguage);
        }

        return config;
    }

    private FieldMask CreateConfigMask(string? language, string? modelId)
    {
        FieldMask mask = new();
        mask.Paths.Add("model");
        if(!string.IsNullOrWhiteSpace(language) || !string.IsNullOrWhiteSpace(options.DefaultLanguageCode))
        {
            mask.Paths.Add("language_codes");
        }

        return mask;
    }

    private string ResolveModel(string? modelId)
    {
        string resolved = string.IsNullOrWhiteSpace(modelId) ? options.DefaultModelId : modelId!;
        return resolved.Contains('/', StringComparison.Ordinal) ? resolved : modelResourceNames.GetOrAdd(resolved, BuildModelResourceName);
    }

    private string BuildModelResourceName(string modelId)
    {
        return $"projects/{options.ProjectId}/locations/{options.Location}/models/{modelId}";
    }

    private string BuildRecognizerName()
    {
        return RecognizerName.FromProjectLocationRecognizer(options.ProjectId, options.Location, options.RecognizerId).ToString();
    }

    private static string? BuildTranscript(IEnumerable<SpeechRecognitionAlternative> alternatives)
    {
        string text = string.Join(" ", alternatives.Select(a => a.Transcript).Where(t => !string.IsNullOrWhiteSpace(t)));
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

}
