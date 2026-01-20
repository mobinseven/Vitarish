using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Intelligence;

public class IntelligenceProcessor(IIntelligenceModule intelligenceModule,
        IOptionsMonitor<AiOptions> aiOptions,
        ModelRegistry modelRegistry,
        IResponder responder,
        ITranscriber transcriber)
{
    private AiOptions CurrentOptions => aiOptions.CurrentValue;

    private const string SelfTestPrompt = "Say 'ok'";
    private static readonly byte[] DummyAudioData = GenerateDummyWebmData();

    public async Task ProcessSelfTests(CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateDefaultModels();
            await ProbeResponderAsync(cancellationToken).ConfigureAwait(false);
            await ProbeTranscriberAsync(cancellationToken).ConfigureAwait(false);

            intelligenceModule.IsAvailable = true;
        }
        catch
        {
            intelligenceModule.IsAvailable = false;
        }
    }

    private void ValidateDefaultModels()
    {
        string defaultResponseModel = CurrentOptions.DefaultResponseModel;
        string defaultTranscriptionModel = CurrentOptions.DefaultTranscriptionModel;

        string? responseProvider = modelRegistry.GetProviderForModel(defaultResponseModel);
        if (string.IsNullOrEmpty(responseProvider))
        {
            throw new InvalidOperationException(
                $"Default response model '{defaultResponseModel}' is not mapped to any known provider in ModelRegistry.");
        }

        string? transcriptionProvider = modelRegistry.GetProviderForModel(defaultTranscriptionModel);
        if (string.IsNullOrEmpty(transcriptionProvider))
        {
            throw new InvalidOperationException(
                $"Default transcription model '{defaultTranscriptionModel}' is not mapped to any known provider in ModelRegistry.");
        }

        AvailableModels availableModels = modelRegistry.GetAvailableModels();

        bool responseModelAvailable = availableModels.ResponderModels.Any(m =>
            string.Equals(m.Id, defaultResponseModel, StringComparison.OrdinalIgnoreCase));

        bool transcriptionModelAvailable = availableModels.TranscriberModels.Any(m =>
            string.Equals(m.Id, defaultTranscriptionModel, StringComparison.OrdinalIgnoreCase));
    }

    private async Task ProbeResponderAsync(CancellationToken cancellationToken)
    {
        try
        {
            ResponderOptions options = new()
            {
                Model = CurrentOptions.DefaultResponseModel
            };

            IReadOnlyList<ResponderInput> input = new[] { ResponderInput.TextInput(SelfTestPrompt) };

            IReadOnlyList<ResponderOutput> response = await responder.GenerateAsync(input, options, cancellationToken)
                .ConfigureAwait(false);

            if (response.Count == 0)
            {
                throw new InvalidOperationException("Responder returned empty response during self-test probe.");
            }

        }
        catch
        {
            throw;
        }
    }

    private async Task ProbeTranscriberAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using MemoryStream audioStream = new(DummyAudioData, writable: false);

            string? result = await transcriber.TranscribeAsync(
                audioStream,
                "audio/webm",
                language: null,
                modelId: CurrentOptions.DefaultTranscriptionModel,
                cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            throw;
        }
    }

    /// <summary>
    /// Generates a minimal valid WebM file with silent audio (or very short audio).
    /// This is a stub — you must fill in a correct WebM container + audio codec header + payload, or embed a small pre-generated WebM resource.
    /// </summary>
    private static byte[] GenerateDummyWebmData() => IntelligenceResource.audio_hello_world_webm;
}
